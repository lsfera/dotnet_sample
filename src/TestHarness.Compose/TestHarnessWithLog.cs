using System;
using NLog;
using NLog.Config;
using NLog.Targets;
using Xunit.Abstractions;
using ILogger = NLog.ILogger;
using LogLevel = NLog.LogLevel;

namespace TestHarness.Compose
{
    public abstract class TestHarnessWithLog :
        IDisposable
    {
        protected readonly ILogger Logger;

        protected TestHarnessWithLog(ITestOutputHelper testOutputHelper)
        {
            Logger = Logging.XUnitLoggerNlog(testOutputHelper);
        }

        public Microsoft.Extensions.Logging.ILogger Logger1 { get; set; }

        public virtual void Dispose()
        {
            LogManager.Shutdown();
        }
    }

    public static class Logging
    {
        public static ILogger XUnitLoggerNlog(ITestOutputHelper testOutputHelper)
        {
            // Step 1. Create configuration object 
            var config = new LoggingConfiguration();

            // Step 2. Create targets and add them to the configuration 
            var consoleTarget = new XUnitTarget(testOutputHelper);
            config.AddTarget("xunit", consoleTarget);

            // Step 3. Set target properties 
            consoleTarget.Layout = @"${date:format=HH\:mm\:ss} ${logger} ${message}";

            // Step 4. Define rules
            var rule1 = new LoggingRule("*", LogLevel.Trace, consoleTarget);
            config.LoggingRules.Add(rule1);

            // Step 5. Activate the configuration
            LogManager.Configuration = config;

            return LogManager.GetLogger("");
        }

        [Target("XUnit")]
        public sealed class XUnitTarget : TargetWithLayout
        {
            private readonly ITestOutputHelper _output;

            public XUnitTarget(ITestOutputHelper testOutputHelper)
            {
                _output = testOutputHelper;
            }

            protected override void Write(LogEventInfo logEvent)
            {
                string logMessage = this.Layout.Render(logEvent);

                _output.WriteLine(logMessage);
            }
        }
    }
}
