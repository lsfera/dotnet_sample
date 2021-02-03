using System;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using Daemon.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NLog;
using RabbitMQ.Client;
using ILogger = NLog.ILogger;

namespace Daemon
{               
    public class Service : IHostedService, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;
        
        private IConnection? _connection;
        private ISession? _session;
        private ConsumerOf<PurchaseOrder>? _consumer;
        private IDisposable? _cluster;



        public Service(IConfiguration configuration, ILogger logger)
        {
            _configuration = configuration;
            _logger = logger;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var (cluster, session) = await BootStrapCassandra(_configuration.For<Configuration.Cassandra>(), _logger).ConfigureAwait(false);
            _session = session;
            _cluster = cluster;
            var rabbitMqConfiguration = _configuration.For<Configuration.RabbitMQ>();
            _connection = BootStrapBroker(rabbitMqConfiguration);

            _consumer = await BootstrapConsumer(session, _connection, rabbitMqConfiguration).ConfigureAwait(false);
               
            _logger.Info("ServiceStarted");
        }

        private async Task<ConsumerOf<PurchaseOrder>> BootstrapConsumer(ISession session,
            IConnection connection,
            Configuration.RabbitMQ rabbitMqConfiguration)
        {
            var consumer = new ConsumerOf<PurchaseOrder>(
                new PurchaseOrderHandler(session,
                        await session.PrepareAsync(PurchaseOrderHandler.InsertCql)
                            .ConfigureAwait(false)),
                rabbitMqConfiguration,
                _cancellationTokenSource.Token,
                _logger);
            consumer.Start(connection);
            return consumer;
        }


        internal static IConnection BootStrapBroker(Configuration.RabbitMQ configuration) 
        => new ConnectionFactory {
                AutomaticRecoveryEnabled = true,
                DispatchConsumersAsync = true,
                TopologyRecoveryEnabled = true,
                ClientProvidedName = typeof(Service).AssemblyQualifiedName,
                Port = configuration.Port,
                VirtualHost = configuration.VirtualHost,
                UserName = configuration.UserName,
                Password = configuration.Password,
                UseBackgroundThreadsForIO = true
            }.CreateConnection(configuration.Hosts);

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.Info("ServiceStopped");
            _cancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();

            try
            {
                _consumer?.Dispose();
                _connection?.Close();
                _connection?.Dispose();
                _session?.Dispose();
                _cluster?.Dispose();
                LogManager.Shutdown();
            }
            catch
            {
                //shallow
            }
        }

        internal static async Task<(IDisposable cluster, ISession session)> BootStrapCassandra(Configuration.Cassandra configuration, ILogger logger)
        {
            var builder = Cluster.Builder()
                .AddContactPoints(configuration.Hosts)
                .WithCredentials(configuration.UserName, configuration.Password);
            var cluster =builder.Build();
            var session = await cluster.ConnectAsync(PurchaseOrderHandler.KeySpace)
                .ConfigureAwait(false);
            
            return (cluster, session);
        }
    }
}