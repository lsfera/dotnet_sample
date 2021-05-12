using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using RabbitMQ.Client;

namespace Daemon
{
    internal class ConsumerOf<T> : AsyncDefaultBasicConsumer,  IDisposable where T : class
    {
        private const String ExchangeType = "fanout";
        private readonly ILogger _logger;
        private readonly IMessageHandlerOf<T> _messageHandler;
        private readonly Configuration.RabbitMQ _configuration;
        private readonly CancellationToken _cancellationToken;

        private IModel? _channel;

        public ConsumerOf(IMessageHandlerOf<T> messageHandler,
                          Configuration.RabbitMQ configuration,
                          CancellationToken cancellationToken,
                          ILogger logger)
        {
            _messageHandler = messageHandler;
            _configuration = configuration;
            _cancellationToken = cancellationToken;
            _logger = logger;
        }

        public override Task HandleBasicDeliver(String consumerTag,
                                                UInt64 deliveryTag,
                                                Boolean redelivered,
                                                String exchange,
                                                String routingKey,
                                                IBasicProperties properties,
                                                ReadOnlyMemory<Byte> body)
            => HandleDeliverInternal(deliveryTag, body, redelivered, properties);

        private async Task HandleDeliverInternal(UInt64 deliveryTag, 
                                                 ReadOnlyMemory<Byte> body, 
                                                 Boolean redelivered,
                                                 IBasicProperties basicProperties)
        {
            try
            {
                await _messageHandler.HandleAsync(
                        System.Text.Json.JsonSerializer.Deserialize<T>(body.Span) ?? throw new ArgumentNullException(nameof(body)), _cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error on handling message");
                if (redelivered)
                    await TryToDlxDelivery(deliveryTag).ConfigureAwait(false);
                else
                    await TryToNAckDelivery(deliveryTag).ConfigureAwait(false);
                return;
            }
            await TryToAckDelivery(deliveryTag).ConfigureAwait(false);
        }
        
        private IModel CreateChannel(IConnection connectionManager)
        {
            var channel = connectionManager.CreateModel();
            try
            {
                var dlQueue = Configuration.RabbitMQ.DlQueue(_configuration);
                var dlExchange = Configuration.RabbitMQ.DlExchange(_configuration);
                var exchange = _configuration.ExchangeName;
                var queue = Configuration.RabbitMQ.Queue(_configuration);

                
                channel.ExchangeDeclare(exchange, ExchangeType, durable:true);
                channel.ExchangeDeclare(dlExchange, ExchangeType, durable: true);
                channel.QueueDeclare(queue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: new Dictionary<String, Object>
                    {
                        { "x-dead-letter-exchange", dlExchange}
                    });

                channel.QueueBind(queue, exchange, String.Empty);

                
                channel.QueueDeclare(dlQueue, true, false, false);
                channel.QueueBind(dlQueue, dlExchange, String.Empty);
                channel.BasicQos(0, _configuration.PrefetchCount.GetValueOrDefault(100), true);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error on channel creation");
                throw;
            }
            return channel;
        }
        
        private async ValueTask TryToAckDelivery(UInt64 deliveryTag)
        {
            try
            {
                _channel?.BasicAck(deliveryTag, false);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error on Ack");
                await TryToNAckDelivery(deliveryTag);
            }
            await ValueTask.CompletedTask;
        }

        private ValueTask TryToNAckDelivery(UInt64 deliveryTag)
        {
            try
            {
                _channel?.BasicNack(deliveryTag, false, true);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error on NAck");
            }
            return ValueTask.CompletedTask;
        }

        private ValueTask TryToDlxDelivery(UInt64 deliveryTag)
        {
            try
            {
                _channel?.BasicNack(deliveryTag, false, false);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error on Dlx");
            }
            return ValueTask.CompletedTask;
        }

        public void Start(IConnection connectionManager)
        {
            _channel = CreateChannel(connectionManager);
            _channel.BasicConsume(Configuration.RabbitMQ.Queue(_configuration), false, typeof(T).Name, this);
        }

        protected virtual void Dispose(Boolean disposing)
        {
            if (!disposing)
                return;

            try
            {
                _channel?.Dispose();
            }
            catch
            {
                // ignored
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~ConsumerOf()
        {
            Dispose(false);
        }
    }
}