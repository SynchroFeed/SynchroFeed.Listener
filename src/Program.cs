#region header
// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Robert Vandehey" file="Program.cs">
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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Extensions.Logging;
using SynchroFeed.Library.DependencyInjection;
using SynchroFeed.Library.Settings;
using Topshelf;
using Topshelf.Runtime.Windows;

namespace SynchroFeed.Listener
{
    /// <summary>
    /// The main program that contains the entry point of the application.
    /// </summary>
    class Program
    {
        const int SERVICE_NO_CHANGE = -1;
        [DllImport("Advapi32.dll", EntryPoint = "ChangeServiceConfigW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static extern bool ChangeServiceConfig(
            SafeHandle hService,
            int dwServiceType,
            int dwStartType,
            int dwErrorControl,
            [In] string lpBinaryPathName,
            [In] string lpLoadOrderGroup,
            IntPtr lpdwTagId,
            [In] string lpDependencies,
            [In] string lpServiceStartName,
            [In] string lpPassWord,
            [In] string lpDisplayName
        );

        /// <summary>
        /// The entry point of the SynchroFeed.Listener application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            var logger = LogManager.GetCurrentClassLogger();

            logger.Log(NLog.LogLevel.Info, "SynchroFeed.Listener Starting");
            logger.Log(NLog.LogLevel.Debug, $"args length: {args.Length}");

            foreach (var arg in args)
            {
                logger.Log(NLog.LogLevel.Debug, $"arg: {arg}");
            }

            var commandLineApp = new ListenerCommandLine();
            Environment.ExitCode = Execute(commandLineApp, logger);
        }

        /// <summary>
        /// The main setup and execution logic for the SynchroFeed.Listener program.
        /// </summary>
        /// <param name="commandLineApp">The command line application containing the parsed command line parameters.</param>
        /// <param name="logger">The NLog logger instance.</param>
        /// <returns>System.Int32.</returns>
        private static int Execute(ListenerCommandLine commandLineApp, Logger logger)
        {
            var rc = HostFactory.Run(x =>
            {
                x.Service<ListenerService>(s =>
                {
                    x.AddCommandLineDefinition("config", f => { commandLineApp.ConfigFile = f;} );
                    x.ApplyCommandLine();
                    s.ConstructUsing(name =>
                    {
                        logger.Log(NLog.LogLevel.Debug, "Calling CreateHostBuilder...");
                        var host = CreateHostBuilder(commandLineApp, logger)
                            .Build();
                        logger.Log(NLog.LogLevel.Debug, "Calling CreateHostBuilder...done");
                        return host.Services.GetRequiredService<ListenerService>();
                    });
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc =>
                    {
                        try
                        {
                            tc.Stop();
                        }
                        finally
                        {
                            LogManager.Shutdown();
                        }
                    });
                });
                x.RunAsLocalSystem();

                x.SetDescription("SynchroFeed Listener");
                x.SetDisplayName("SynchroFeed Listener");
                x.SetServiceName("SynchroFeedListener");

                x.AfterInstall(installHostSettings =>
                {
                    using (var scmHandle = NativeMethods.OpenSCManager(null, null, (int)NativeMethods.SCM_ACCESS.SC_MANAGER_ALL_ACCESS))
                    {
                        using (var serviceHandle = NativeMethods.OpenService(scmHandle, installHostSettings.ServiceName, (int)NativeMethods.SCM_ACCESS.SC_MANAGER_ALL_ACCESS))
                        {
                            var exePath = Process.GetCurrentProcess().MainModule.FileName + $" -config:{commandLineApp.GetFullPathConfigFile()}";
                            ChangeServiceConfig(serviceHandle,
                                                SERVICE_NO_CHANGE,
                                                SERVICE_NO_CHANGE,
                                                SERVICE_NO_CHANGE,
                                                exePath,
                                                null,
                                                IntPtr.Zero,
                                                null,
                                                null,
                                                null,
                                                null);
                        }
                    }
                });
            });

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
            return exitCode;
        }

        /// <summary>
        /// Creates the host builder and initializes the application.
        /// </summary>
        /// <param name="commandLineApp">The parsed command line arguments.</param>
        /// <param name="logger">The NLog logger instance.</param>
        /// <returns>Returns an instance of IHostBuilder.</returns>
        private static IHostBuilder CreateHostBuilder(ListenerCommandLine commandLineApp, Logger logger)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    logger.Log(NLog.LogLevel.Debug, $"Using config file: {commandLineApp.GetFullPathConfigFile()}");

                    var configFilename = commandLineApp.GetFullPathConfigFile();
                    if (!File.Exists(configFilename))
                    {
                        logger.Log(NLog.LogLevel.Fatal, $"Configuration file not found {configFilename}. Aborting...");
                        Environment.Exit(1);
                    }

                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile(configFilename);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();
                    services.AddSingleton(commandLineApp);
                    services.Configure<ApplicationSettings>(hostContext.Configuration.GetSection("FeedSettings"));
                    services.Configure<Settings.AwsSettings>(hostContext.Configuration.GetSection("AwsSettings"));
                    services.AddSingleton(provider => provider.GetService<IOptions<Settings.AwsSettings>>().Value);
                    services.AddSingleton(typeof(ListenerService));
                    services.AddSynchroFeed();
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    logging.AddNLog();
                });

            return builder;
        }
    }
}
