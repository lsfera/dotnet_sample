using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using Daemon.Configuration;
using Microsoft.Extensions.Configuration;
using TestHarness.Compose;
using Xunit;
using Xunit.Abstractions;

namespace Daemon.IntegrationTests
{
    [Collection(nameof(Compose.Collection))]
    // ReSharper disable once InconsistentNaming
    public class End_To_End :  ComposeTestHarness<Cassandra, RabbitMQ>
    {
        private readonly Service _service;
        private readonly IConfiguration _configuration;

        public End_To_End(ITestOutputHelper testOutputHelper, Compose.With<Cassandra, RabbitMQ> fixture)
            : base(testOutputHelper, fixture)
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
            _service = new Service(_configuration, Logger);
        }

        [Fact]
        public async Task The_happy_path()
        {
            var cancellationToken = CancellationToken.None;
            await _service.StartAsync(cancellationToken).ConfigureAwait(false);
            var mqConfiguration = _configuration.For<Configuration.RabbitMQ>();

            var message = MessageStub(1);
            MessagePublish(mqConfiguration, message);

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

            //Retrieve data from Cassandra
            var rows = await RetrieveData(_configuration.For<Configuration.Cassandra>()).ConfigureAwait(false);
            var _ = rows.Single();

            //Verify correct mapping of data committed to cassandra
            //Remember: Timestamp on cassandra has millisecond resolution
            Assert.Equal(message.Id, _.GetValue<String>("id"));
            Assert.Equal(message.CountryIsoCode, _.GetValue<String>("country"));
            Assert.Equal(message.Currency, _.GetValue<String>("currency"));
            Assert.Equal(message.Amount, _.GetValue<Decimal>("amount"));
            Assert.Equal(message.Status, _.GetValue<String>("status"));
            Assert.Equal(message.CreatedAt.ToUnixTimeMilliseconds(), _.GetValue<DateTimeOffset>("created_at").ToUnixTimeMilliseconds());

            //The broker was ack-ed with no errors
            Assert.Equal(0U, GetMessageCount(mqConfiguration, Configuration.RabbitMQ.Queue(mqConfiguration)));
            Assert.Equal(0U, GetMessageCount(mqConfiguration, Configuration.RabbitMQ.DlQueue(mqConfiguration)));

        }

        private async Task<List<Row>> RetrieveData(Configuration.Cassandra configuration)
        {
            var (_, session) = await Service.BootStrapCassandra(configuration, Logger).ConfigureAwait(false);
            try
            {
                using var rowSet = await session.ExecuteAsync(
                    new SimpleStatement($"SELECT * FROM {PurchaseOrderHandler.TableName};")).ConfigureAwait(false);
                var rows = rowSet.GetRows().ToList();
                return rows;
            }
            finally
            {
                session.Dispose();
            }
        }

        private void MessagePublish(Configuration.RabbitMQ configuration, PurchaseOrder order)
        {
            using var connection = Service.BootStrapBroker(configuration);
            var model = connection.CreateModel();
            model.BasicPublish(configuration.ExchangeName, "", true, null,
                System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(order));
        }

        private UInt32 GetMessageCount(Configuration.RabbitMQ configuration, String queueName)
        {
            using var connection = Service.BootStrapBroker(configuration);
            using var channel = connection.CreateModel();
            return channel.MessageCount(queueName);
        }

        private static PurchaseOrder MessageStub(Int32 i) =>
            new()
            {
                Id = $"order{i}",
                Amount = 1,
                CountryIsoCode = "IT",
                Currency = "EUR",
                CreatedAt = DateTimeOffset.UtcNow
            };

        public override void Dispose()
        {
            _service?.Dispose();
            base.Dispose();
        }
    }
}
