using System;

#pragma warning disable 8618
namespace Daemon
{
    public class PurchaseOrder
    {
        public String Id { get; init; }
        public String CountryIsoCode { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public Decimal Amount { get; init; }
        public String Currency { get; init; }
        public String Status => "Created";

        public void Deconstruct(out String orderId, out String country, out DateTimeOffset createdAt, out Decimal orderAmount, out String currency, out String orderStatus)
        {
            orderId = Id;
            country = CountryIsoCode;
            createdAt = CreatedAt;
            orderAmount = Amount;
            currency = Currency;
            orderStatus = Status;
        }
    }
}