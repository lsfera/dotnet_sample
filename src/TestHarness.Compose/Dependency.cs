using System;
using Ductus.FluentDocker.Services;
using Microsoft.Extensions.Configuration;

namespace TestHarness.Compose
{
    public abstract class Dependency
    {
        internal readonly String FileName;
        internal readonly ValueTuple<String, Func<IContainerService, Int32, Int32>> ReadinessProbe;
        protected readonly IConfiguration Configuration;

        protected Dependency((String, Func<IContainerService, Int32, Int32>) readinessProbe)
        {
            FileName = $"{GetType().Name.ToLowerInvariant()}.docker-compose.yml";
            ReadinessProbe = readinessProbe;
            Configuration = new ConfigurationBuilder()
              .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
              .AddJsonFile("appsettings.json")
              .AddEnvironmentVariables()
              .Build();
        }
    }
}
