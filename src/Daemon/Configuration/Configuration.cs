using System;

#pragma warning disable 8618

namespace Daemon.Configuration
{
    internal interface IHasConfiguration
    {
        String Key { get; }
    }

    internal class Credentials
    {
        public String UserName { get; init; }
        public String Password { get; init; }
    }

    internal class HostsCredentials : Credentials
    {
        public String[] Hosts { get; init; }
    }

    internal class Cassandra : HostsCredentials, IHasConfiguration
    {
        public String Key => nameof(Cassandra);
    }

    internal class RabbitMQ : HostsCredentials, IHasConfiguration
    {
        internal static readonly Func<RabbitMQ, String> Queue = _ => $"{_.QueueName}";
        internal static readonly Func<RabbitMQ, String> DlExchange = _ => $"{_.QueueName}:dlx";
        internal static readonly Func<RabbitMQ, String> DlQueue = _ => $"{_.QueueName}:dlq";
        public String Key => nameof(RabbitMQ);
        public Int32 Port { get; init; }
        public String ExchangeName { get; init; }
        public String QueueName { get; init; }
        public String VirtualHost { get; init; }
        public UInt16? PrefetchCount { get; init; }
    }
}
