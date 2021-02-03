using System;
using Daemon.Configuration;
using Ductus.FluentDocker.Services;
using RabbitMQ.Client;
using TestHarness.Compose;

namespace Daemon.IntegrationTests
{
    public class RabbitMQ
      : Dependency
    {
        private static Configuration.RabbitMQ _rabbitMq;
        public RabbitMQ() : base(("rabbitmq", (service, i) => RabbitMqProbe(i, service)))
        {
            _rabbitMq = Configuration.For<Configuration.RabbitMQ>();
        }
        private static Int32 RabbitMqProbe(in Int32 i, IContainerService service)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    Port = _rabbitMq.Port,
                    VirtualHost = _rabbitMq.VirtualHost,
                    UserName = _rabbitMq.UserName,
                    Password = _rabbitMq.Password,
                };
                using var connection = factory.CreateConnection(_rabbitMq.Hosts);
                return 0;
            }
            catch
            {
                return 500;
            }
        }
    }
}
