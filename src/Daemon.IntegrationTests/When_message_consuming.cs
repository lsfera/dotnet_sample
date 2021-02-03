using System;
using System.Threading;
using System.Threading.Tasks;
using Daemon.Configuration;
using Microsoft.Extensions.Configuration;
using Moq;
using RabbitMQ.Client;
using TestHarness.Compose;
using Xunit;
using Xunit.Abstractions;

namespace Daemon.IntegrationTests
{
    [Collection(nameof(Compose.Collection))]
    public class When_message_consuming : ComposeTestHarness<Cassandra, RabbitMQ>
    {
        private readonly IConfiguration _configuration;
        private ConsumerOf<PurchaseOrder> _sut;
        private readonly Guid _messageId = Guid.NewGuid();
        private readonly Int64 _referralDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        

        public When_message_consuming(ITestOutputHelper testOutputHelper, Compose.With<Cassandra, RabbitMQ> fixture) : base(testOutputHelper, fixture)
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
        }
        
        //Message gets NACK-ed(redelivered), consumed, ACK-ed
        [Fact]
        public async Task Fails_once()
        {
            var messageHandler = new Mock<IMessageHandlerOf<PurchaseOrder>>();
            messageHandler.SetupSequence(_ =>
                    _.HandleAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception())
                .Returns(Task.CompletedTask);


            var mqConfiguration = _configuration.For<Configuration.RabbitMQ>();

            var cancellationToken = CancellationToken.None;
            _sut = new ConsumerOf<PurchaseOrder>(messageHandler.Object,
                mqConfiguration, cancellationToken, Logger);

            _sut.Start(Service.BootStrapBroker(mqConfiguration));


            var message = MessageStub(1);

            MessagePublish(message, mqConfiguration);


            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

            messageHandler.Verify(_ =>
                    _.HandleAsync(It.Is<PurchaseOrder>(__ => __.Id == message.Id), cancellationToken),
                    Times.Exactly(2));
            //The broker was ack-ed with no errors
            Assert.Equal(0U, GetMessageCount(Configuration.RabbitMQ.Queue(mqConfiguration)));
            Assert.Equal(0U, GetMessageCount(Configuration.RabbitMQ.DlQueue(mqConfiguration)));
        }
 

        //Message gets NACK-ed(redelivered) and finally DLX-ed
        [Fact]
        public async Task Fails_twice()
        {
            var cancellationToken = CancellationToken.None;
            var message = MessageStub(1);

            var messageHandler = new Mock<IMessageHandlerOf<PurchaseOrder>>();
            messageHandler.SetupSequence(handler =>
                    handler.HandleAsync(It.IsAny<PurchaseOrder>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception())
                .ThrowsAsync(new Exception());


            var mqConfiguration = _configuration.For<Configuration.RabbitMQ>();

            _sut = new ConsumerOf<PurchaseOrder>(messageHandler.Object,
                mqConfiguration, cancellationToken, Logger);

            _sut.Start(Service.BootStrapBroker(mqConfiguration));

            MessagePublish(message, mqConfiguration);

            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

            messageHandler.Verify(handler =>
                    handler.HandleAsync(It.Is<PurchaseOrder>(__ => __.Id == message.Id), cancellationToken),
                Times.Exactly(2));

            //The broker was dlx-ed with no errors
            Assert.Equal(0U, GetMessageCount(Configuration.RabbitMQ.Queue(mqConfiguration)));
            Assert.Equal(1U, GetMessageCount(Configuration.RabbitMQ.DlQueue(mqConfiguration)));
        }

        private void MessagePublish(PurchaseOrder order, Configuration.RabbitMQ rabbitMqConfiguration)
        {
            using var connection = Service.BootStrapBroker(rabbitMqConfiguration);
            var model = connection.CreateModel();
            var basicProperties = model.CreateBasicProperties();
            basicProperties.ContentType = "application/json";
            basicProperties.ContentEncoding = "UTF-8";
            basicProperties.MessageId = _messageId.ToString();
            basicProperties.Timestamp = new AmqpTimestamp(_referralDate);
            basicProperties.Type = "urn:message:fulfillment:purchaseorder";
            model.BasicPublish(rabbitMqConfiguration.ExchangeName, "", true, basicProperties,
                Utf8Json.JsonSerializer.Serialize(order));
        }

        private UInt32 GetMessageCount(String queueName)
        {
            using var connection = Service.BootStrapBroker(_configuration.For<Configuration.RabbitMQ>());
            using var channel = connection.CreateModel();
            return channel.MessageCount(queueName);
        }

        private static PurchaseOrder MessageStub(Int32 i) =>
            new()
            {
                Id = $"order{i}",
                Amount = 1,
                CreatedAt = DateTimeOffset.UtcNow
            };

        public override void Dispose()
        {
            _sut?.Dispose();
            base.Dispose();
        }
    }
}
