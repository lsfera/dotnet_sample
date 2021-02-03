using Microsoft.Extensions.Configuration;

namespace Daemon.Configuration
{
    public static class ConfigurationExtensions
    {
        internal static T For<T>(this IConfiguration configuration) where T : class, IHasConfiguration, new()
        {
            var instance = new T();
            configuration.GetSection(instance.Key).Bind(instance);
            return instance;
        }
    }
}