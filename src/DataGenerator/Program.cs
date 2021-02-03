using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Carrot;
using Carrot.Configuration;
using Carrot.Messages;

namespace DataGenerator
{
    static class Program
    {
        // ReSharper disable once InconsistentNaming
        private const String RabbitMQUri = "amqp://guest:guest@rabbitmq:5672/Fulfillment";
        private const String Exchange = "order:purchase"; // provided by compose bootstrapper

        static async Task Main(String[] args)
        {
            if (!Int32.TryParse(Environment.GetEnvironmentVariable("MESSAGES") ?? args.SingleOrDefault(), out var providedMessageNo))
                do
                    await Console.Out
                        .WriteLineAsync("How many messages do you want to publish?")
                        .ConfigureAwait(false);
                while (!Int32.TryParse(await Console.In.ReadLineAsync().ConfigureAwait(false),
                        out providedMessageNo) && providedMessageNo > 0);
            await Console.Out.WriteLineAsync($"\r\nPublishing {providedMessageNo} {nameof(PurchaseOrder)} messages on '{RabbitMQUri}'");
            var broker = BuildBroker;
            var exchange = DeclareExchange(broker);
            using var connection = broker.Connect();
            foreach (var country in new[] {"IT", "FR"})
                Enumerable.Range(1, providedMessageNo)
                    .ToList()
                    .ForEach(async _
                        => await connection.PublishAsync(BuildMessage(_, country), exchange)
                            .ConfigureAwait(false));
            await Console.Out.WriteLineAsync("\r\nDone.");
            await Console.In.ReadLineAsync();
        }

        private static IBroker BuildBroker =>
            Broker.New(_ =>
            {
                _.Endpoint(new Uri(RabbitMQUri, UriKind.Absolute));
                _.ResolveMessageTypeBy(
                    new MessageBindingResolver(typeof(PurchaseOrder).GetTypeInfo().Assembly));
                _.PublishBy(OutboundChannel.Reliable());
            });

        private static Exchange DeclareExchange(IBroker broker) => broker.DeclareDurableFanoutExchange(Exchange);


        private static OutboundMessage<PurchaseOrder> BuildMessage(Int32 i, String country)
        {
            var dateTimeOffset = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(1));
            return new DurableOutboundMessage<PurchaseOrder>(new PurchaseOrder
            {
                Amount = 10,
                CountryIsoCode = country,
                Currency = "EUR",
                Id = $"order{i}",
                ExecutionDate = dateTimeOffset.AddSeconds(i)
            });
        }
    }
}
