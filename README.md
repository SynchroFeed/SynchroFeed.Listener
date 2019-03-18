# SynchroFeed Listener
*SynchroFeed* is an extensible framework written in Microsoft .NET C# for integrating with a Nuget-like feed. 
The *SynchroFeed.Listener* program is a command line program or Windows service that exposes the features of 
the *SynchroFeed Library* through configuration. 

The difference between the *SynchroFeed.Listener* and the *SynchroFeed.Console* program
is the *Console* program operates on all of the packages in a feed processing and syncing all packages on a feed that match
the configuration. The *Listener* program instead listens to an AWS SQS queue for messages of feed changes like packages
added or deleted and processes or syncs the particular package based on the event. This enables real-time processing
of packages on a feed which enables immediate actions like syncing to occur as soon as the feed has been updated.

The *Listener* leverages the [webhook](https://inedo.com/support/documentation/proget/advanced/webhooks) feature of 
Inedo's Proget server to send messages. The [*SynchroFeed.AWS.Lambda.WebHook*](https://github.com/SynchroFeed/SynchroFeed.SynchroFeed.AWS.Lamba.WebHook)
Github project contains an implementation of a handler for the webhook that puts the message into an
AWS SNS queue that sends the message to a subscribed AWS SQS queue for delivery to the *Listener*.

The following documents the command line and configuration options for the *Listener* program.

## Command Line Options
The *Listener* can run as a Windows Service or command line program and supports a couple command line arguments:

```
SynchroFeed.Listener.exe [-config:<configFilename>] [-?|-h|--help]
```
NOTE: The ```-config``` command line option is slightly different than the *Console* switch due to the *Listener* using
the Topshelf library which does its own command line handling. See the [Topshelf documentation](http://docs.topshelf-project.com/en/latest/overview/commandline.html)
for additional command line switches and options.

If no command line arguments are passed, then *Listener* will run as a console program and look for a file named `app.json` 
in the current directory. The *Listener* will then wait for messages to be received from the AWS SQS queue.

## Installing as a Service
The *Listener* program can be installed as a Windows Service using Topshelf by executing the following command:

```
SynchroFeed.Listener.exe install -username "DOMAIN\Service Account" -password "Its A Secret" –autostart
```
See the [Topshelf documentation](http://docs.topshelf-project.com/en/latest/overview/commandline.html)
for additional command line switches and options.

## Configuration
The *Listener* program is driven by a JSON configuration file that supports the following configuration:

- Configuring feeds leveraging plugins such as ```Nuget```, ```Proget``` and ```Directory``` plugins that implement the ```IRepository``` interface
- Configuring ```Actions``` using plugins such as ```Sync``` and ```Process``` that implement the ```IAction``` interface
- Adding ```Commands``` to ```Actions``` to process or validate packages processed by the ```Action```. 
Plugins such as ```Catalog``` and ```ApplicationIs64bit``` and others that implement the ```ICommand``` interface can be associated to an ```Action``` and chained together.
- Adding ```Observers``` to ```Actions``` to handle processing and validation events such as sending a message to Slack.

With the exception of AWS Settings configuration, the configuration for the *Listener* is exactly the same as for the *Console* program. 
See the [*Console* readme](https://github.com/SynchroFeed/SynchroFeed.Console/blob/master/README.md) for details on the 
configuration.

## AWS Settings Configuration
The app.json file contains a node for AwsSettings with options to config the AWS credentials and the name of the SQS queue and region.
The configuration node has the following elements:

```
  "AwsSettings": {
    "Credentials": {
      "Type": "Basic",
      "AccessKey": "MyAWSAccessKey",
      "SecretKey": "MyAWSSecretKey"
    },
    "Sqs": {
      "Name": "AWSSqsQueueName",
      "Region": "us-east-1"
    }
  }
```

The following section documents the different key/value pairs in each section.

#### AWS Credentials
The Credentials section contains the credentials to use to authenticate to AWS. This credential must have the appropriate IAM
permissions to read from the queue. This section is optional. If it is omitted, the credentials stored in the AWS profile will
be used. If not profile exists then an authentication error will occur.

| Property        | Required? | Description
|-----------------|-----------|------------
| Type            | Yes*      | Typically this will always be ```Basic```
| AccessKey       | Yes*      | The IAM Access Key
| SecretKey       | Yes*      | The IAM Secret Key associated with the Access Key

*While the Credentials section is optional, if the Credentials section is included, then the values above are required.

#### AWS SQS
The SQS section contains the name of the SQS queue and AWS region to listen for messages. The queue must exist and the
credentials must have permissions to read from the queue.

| Property        | Required? | Description
|-----------------|-----------|------------
| Name            | Yes       | This is the name of the queue to listen for messages
| Region          | Yes       | The region the named queue is in. This supports any valid AWS region name.

## Messages
The *Listener* listens for messages on the configured AWS SQS queue. The message is expected to match the [FeedEvent model](src/Model/FeedEvent.cs).
Below is an example of a correctly formatted JSON message that matches the model:

```
{
    "packageUrl": "https://myprogetserver.mycompany.com/feeds/MyFeedName/MyPackageId/1.0.0",
    "feed": "MyFeedName",
    "package": "MyPackageId",
    "version": "1.0.0",
    "hash": "a0f99da1bff470df3cd5f98d1853429a4d1f978c",
    "packageType": "nuget",
    "event": "added",
    "user": "MyUsername"
}
```

## Mapping a message to an ```Action```
While the *Listener* and the *Console* program use the same configuration format, the two programs work differently.
The *Console* program works by reading all of the ```Actions``` in the configuration file and iterating through each of the enabled ```Actions```.
The *Listener* program is different. Since it must handle a message in real-time, it instead reads the message from the queue and maps 
the message to the enabled ```Actions``` based on the name of the ```feed``` in the message and the ```SourceFeed``` value assigned to the ```Action```.

For example, in the Message above, the feed name is ```MyFeedName```. When that message is received, the *Listener* will iterate
through all of the configured ```Actions``` that are enabled looking for ```Actions``` where the SourceFeed value matches the feed name from
the message. If there is a match, then the ```Action``` is called for the package detailed in the message.

## Logging
The *Listener* leverages NLog for its logging. The NLog.config file contains the XML configuration for NLog. The default logging
writes to the console and to a file called C:\logs\synchrofeed.listener.log. The default logging is ```Info```. The file is set
to roll over everyday and to only keep 7 archive files. This information can be changed by referring to the 
[NLog configuration documentation](https://github.com/nlog/nlog/wiki/Configuration-file).