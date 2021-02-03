using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using NLog.Extensions.Logging;
using Host = Microsoft.Extensions.Hosting.Host;
using ILogger = NLog.ILogger;

namespace Daemon
{
    static class Program
    {
        private const String ServiceName = "Daemon";

        static Program()
        {
            AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private static (IConfiguration configuration, Logger logger) Configure()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            var logger = LogManager.Setup()
                .SetupExtensions(s => s.AutoLoadAssemblies(false))
                .SetupExtensions(s => s.RegisterConfigSettings(configuration))
                .LoadConfigurationFromSection(configuration)
                .GetCurrentClassLogger();
            return (configuration, logger);
        }

        private static void HandleUnhandledException(Object sender, UnhandledExceptionEventArgs e) =>
            Configure().logger.Fatal((Exception) e.ExceptionObject, ServiceName);

        private static void OnUnobservedTaskException(Object? sender, UnobservedTaskExceptionEventArgs e) =>
            Configure().logger.Fatal(e.Exception?.GetBaseException(), ServiceName);

        private static async Task Main(String[] args)
        {
            var (configuration, logger) = Configure();
            await CreateHostBuilder(args, configuration, logger)
                .Build()
                .RunAsync()
                .ConfigureAwait(false);
        }

        private static IHostBuilder CreateHostBuilder(String[] args, IConfiguration config, ILogger logger) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services)
                    => services.AddHostedService(_ => BuildService(config, logger)))
                               .ConfigureLifeTime();
        
        private static Service BuildService(IConfiguration config, ILogger logger) => new(config, logger);
    }
}
