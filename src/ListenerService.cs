#region header
// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Robert Vandehey" file="ListenerService.cs">
// MIT License
// 
// Copyright(c) 2018 Robert Vandehey
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Amazon;
using Amazon.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SynchroFeed.Library;
using SynchroFeed.Library.Model;
using Newtonsoft.Json;
using SynchroFeed.Library.Processor;
using SynchroFeed.Library.Settings;
using SynchroFeed.Listener.Model;
using SynchroFeed.Listener.Settings;
using SNS = Amazon.SimpleNotificationService;
using SQS = Amazon.SQS;

namespace SynchroFeed.Listener
{
    /// <summary>
    /// The ListenerService is a service that listens for events being raised by a Proget
    /// feed due to a change in the contents of the feed due to events list adding, removing 
    /// or promoting a package.
    /// </summary>
    public class ListenerService
    {
        private SQS.AmazonSQSClient sqsClient;
        private string sqsQueueUrl;
        private bool stopping;
        private Thread listenerThread;

        /// <summary>
        /// Initializes a new instance of the <see cref="ListenerService"/> class.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="actionProcessor">The action processor.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="awsSettings">The AWS settings.</param>
        /// <param name="applicationSettings">The application settings.</param>
        public ListenerService(IServiceProvider serviceProvider,
                                IActionProcessor actionProcessor,
                                ILoggerFactory loggerFactory,
                                AwsSettings awsSettings,
                                ApplicationSettings applicationSettings)
        {
            ServiceProvider = serviceProvider;
            Logger = loggerFactory.CreateLogger<ListenerService>();
            AwsSettings = awsSettings;
            ApplicationSettings = applicationSettings;
            ActionProcessor = actionProcessor;
        }

        /// <summary>
        /// Gets the action processor.
        /// </summary>
        /// <value>The action processor.</value>
        public IActionProcessor ActionProcessor { get; }

        /// <summary>
        /// Gets the application settings.
        /// </summary>
        /// <value>The application settings.</value>
        public ApplicationSettings ApplicationSettings { get; }

        /// <summary>
        /// Gets the AWS settings.
        /// </summary>
        /// <value>The AWS settings.</value>
        public AwsSettings AwsSettings { get; }

        /// <summary>
        /// Gets the service provider.
        /// </summary>
        /// <value>The service provider.</value>
        public IServiceProvider ServiceProvider { get; }

        public ILogger Logger { get; }

        /// <summary>
        /// Starts the service.
        /// </summary>
        public void Start()
        {
            try
            {
                Logger.LogInformation("AWS Listener Starting...");
                InitializeAWSListener();
                Logger.LogInformation("AWS Listener Started");
                listenerThread = new Thread(AWSMessageListener);
                listenerThread.Start();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected exception occurred.");
                throw;
            }
        }

        /// <summary>
        /// Stops the service.
        /// </summary>
        public void Stop()
        {
            Logger.LogInformation("Stopping service");
            stopping = true;
            listenerThread.Join(TimeSpan.FromMinutes(2));
            Logger.LogInformation("Service stopped");
        }

        /// <summary>
        /// Initializes the AWS listener.
        /// </summary>
        private void InitializeAWSListener()
        {
            var credentials = GetAwsCredentials();
            sqsQueueUrl = GetSqsQueueUrl(credentials);
        }

        /// <summary>
        /// Gets the AWS credentials.
        /// </summary>
        /// <returns>BasicAWSCredentials.</returns>
        private BasicAWSCredentials GetAwsCredentials()
        {
            var basicAwsCredentials = new BasicAWSCredentials(AwsSettings.Credentials.AccessKey, AwsSettings.Credentials.SecretKey);
            return basicAwsCredentials;
        }

        /// <summary>
        /// Creates the SQS queue. If the queue already exists, the existing URL is returned.
        /// </summary>
        /// <param name="awsCredentials">The AWS credentials for access the queue.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.InvalidOperationException">Unable to create SQS queue</exception>
        private string GetSqsQueueUrl(AWSCredentials awsCredentials)
        {
            Logger.LogDebug($"Getting SQS Queue: {AwsSettings.Sqs.Name}");
            Logger.LogDebug($"Using AWS Region: {AwsSettings.Sqs.Region}");
            sqsClient = new SQS.AmazonSQSClient(awsCredentials, RegionEndpoint.GetBySystemName(AwsSettings.Sqs.Region));
            var queueUrlResponse = AsyncTaskHelper.RunSync(() => sqsClient.GetQueueUrlAsync(AwsSettings.Sqs.Name));
            Logger.LogTrace($"SQS Queue Response[Status:{queueUrlResponse.HttpStatusCode}], [QueueUrl:{queueUrlResponse.QueueUrl}]");
            if (queueUrlResponse.HttpStatusCode != HttpStatusCode.OK)
            {
                Logger.LogError($"Error getting SQS Queue. Status code: {queueUrlResponse.HttpStatusCode}. Aborting.");
                throw new InvalidOperationException("Unable to open SQS queue");
            }

            return queueUrlResponse.QueueUrl;
        }

        /// <summary>
        /// The main entry point for the thread that listens to messages.
        /// </summary>
        private void AWSMessageListener()
        {
            // Used to eliminate over logging
            bool receivedMessage = true;
            while (!stopping)
            {
                try
                {
                    var receiveMessageRequest = new SQS.Model.ReceiveMessageRequest()
                    {
                        WaitTimeSeconds = 10,
                        QueueUrl = sqsQueueUrl
                    };
                    if (receivedMessage)
                        Logger.LogTrace("Waiting for message from queue");
                    receivedMessage = false;
                    var messageResponse = AsyncTaskHelper.RunSync(() => sqsClient.ReceiveMessageAsync(receiveMessageRequest));

                    if (messageResponse.HttpStatusCode == HttpStatusCode.OK
                        && messageResponse.Messages.Count > 0
                        && !stopping)
                    {
                        receivedMessage = true;
                        Logger.LogDebug($"Received {messageResponse.Messages.Count} message(s)");
                        foreach (var messageResponseMessage in messageResponse.Messages)
                        {
                            try
                            {
                                ProcessMessage(messageResponseMessage);
                            }
                            finally
                            {
#if (!DONTDELETEMESSAGE)
                                // Delete the message even in error since it will keep coming back
                                // Perhaps a better action would be to put in a dead letter queue
                                DeleteMessage(messageResponseMessage);
#endif
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("Error processing message: {0}", ex);
                }
            }

            // Process the received message
            void ProcessMessage(SQS.Model.Message messageResponseMessage)
            {
                Logger.LogTrace($"Received Message ({messageResponseMessage.MessageId}): {messageResponseMessage.Body}");
                var snsMessage = SNS.Util.Message.ParseMessage(messageResponseMessage.Body);
                var feedEvent = JsonConvert.DeserializeObject<FeedEvent>(snsMessage.MessageText);
                Logger.LogInformation($"Feed: {feedEvent.Feed}, Event: {feedEvent.Event}, Package: {feedEvent.Package}, Version: {feedEvent.Version}");
                Logger.LogTrace($"Package URL: {feedEvent.PackageUrl}");

                var actionableActions =
                    ApplicationSettings.Actions.Where(action => string.Equals(action.SourceFeed, feedEvent.Feed, StringComparison.InvariantCultureIgnoreCase) && action.Enabled);
                Package retrievedPackage = null;
                foreach (var actionableAction in actionableActions)
                {
                    Logger.LogTrace($"Found Action ({actionableAction.Name}) for {feedEvent.Feed}");
                    if (feedEvent.Version.IsPrerelease() && !actionableAction.IncludePrerelease)
                    {
                        Logger.LogDebug($"{actionableAction.Name} is ignoring pre-release package ({feedEvent.Package}.{feedEvent.Version})");
                        continue;
                    }

                    using (var scope = ServiceProvider.CreateScope())
                    {
                        var action = ActionProcessor.CreateAction(scope, actionableAction);
                        switch (feedEvent.Event)
                        {
                            case EventType.Added:

                                if (retrievedPackage == null)
                                    retrievedPackage = AsyncTaskHelper.RunSync(() => action.SourceRepository.FetchAsync(feedEvent.Package, feedEvent.Version));

                                if (retrievedPackage == null)
                                {
                                    Logger.LogWarning($"{feedEvent.Package}.{feedEvent.Version} not found in feed {feedEvent.Feed}. Ignoring.");
                                }
                                else
                                {
                                    AsyncTaskHelper.RunSync(() => action.ProcessPackageAsync(retrievedPackage, PackageEvent.Added));
                                }
                                break;
                            case EventType.Deleted:
                            case EventType.Purged:
                                // We can't retrieve the package because it has been deleted
                                var deletePackage = new Package
                                {
                                    Id = feedEvent.Package,
                                    Version = feedEvent.Version
                                };
                                AsyncTaskHelper.RunSync(() => action.ProcessPackageAsync(deletePackage, PackageEvent.Deleted));
                                break;
                        }
                    }
                }
            }

#if (!DONTDELETEMESSAGE)
            // Delete the processed message from the queue
            void DeleteMessage(SQS.Model.Message messageResponseMessage)
            {
                var deleteMessageRequest = new SQS.Model.DeleteMessageRequest
                {
                    QueueUrl = sqsQueueUrl,
                    ReceiptHandle = messageResponseMessage.ReceiptHandle
                };
                Logger.LogTrace($"Deleting Message: {messageResponseMessage.ReceiptHandle}");
                var deleteMessageResponse = AsyncTaskHelper.RunSync(() => sqsClient.DeleteMessageAsync(deleteMessageRequest));
                if (deleteMessageResponse.HttpStatusCode == HttpStatusCode.OK)
                {
                    Logger.LogTrace($"Delete Message: {messageResponseMessage.ReceiptHandle}");
                }
                else
                {
                    Logger.LogWarning($"Unable to delete Message: {messageResponseMessage.ReceiptHandle}. Ignoring.");
                }
            }
#endif
        }
    }
}
