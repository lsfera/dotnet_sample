using System;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;

namespace Daemon
{
    internal class PurchaseOrderHandler : IMessageHandlerOf<PurchaseOrder>
    {
        private readonly ISession _session;
        private readonly PreparedStatement _preparedStatement;
        internal const String TableName = "orders";
        public const String KeySpace = "fulfillment";
        public static readonly String InsertCql =
            $"INSERT INTO {TableName} (id, country, created_at, amount, currency, status) values (?, ?, ?, ?, ?, ?);";

        public PurchaseOrderHandler(ISession session, PreparedStatement preparedStatement)
        {
            _session = session;
            _preparedStatement = preparedStatement;
        }

        public async Task HandleAsync(PurchaseOrder order, CancellationToken token)
        {
            var (orderId, country, createdAt, orderAmount, currency, orderStatus) = order;
            var batch = new BatchStatement(); 
            batch.Add(BindStatement(orderId, country, createdAt, orderAmount, currency, orderStatus));
            await _session.ExecuteAsync(batch.SetIdempotence(true)).ConfigureAwait(false);
        }

        private Statement BindStatement(String orderNumber,
            String country,
            DateTimeOffset createdAt,
            Decimal orderAmount,
            String currency,
            String orderStatus)
            => _preparedStatement.Bind(orderNumber, country, createdAt, orderAmount, currency, orderStatus);
    }
}