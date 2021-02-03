using System;
using System.Collections.Generic;
using Carrot.Configuration;
#pragma warning disable 8618

namespace DataGenerator
{
    [MessageBinding("urn:message:fulfillment:purchaseorder")]
    public class PurchaseOrder
    {
        public String Id { get; init; }
        public String CountryIsoCode { get; init; }
        public DateTimeOffset ExecutionDate { get; init; }
        public Decimal Amount { get; init; }
        public String Currency { get; init; }
    }
}