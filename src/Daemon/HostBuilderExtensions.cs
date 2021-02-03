using System;
using Microsoft.Extensions.Hosting;

namespace Daemon
{
    public static class HostBuilderExtensions
    {
        private static Boolean InDocker => Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
        public static IHostBuilder ConfigureLifeTime(this IHostBuilder hostBuilder)
            => InDocker ? hostBuilder.UseConsoleLifetime() : hostBuilder.UseWindowsService();
    }
}