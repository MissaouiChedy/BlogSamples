using Azure;
using Azure.Data.Tables;

namespace IdempotencyTechnique2
{
    /*
     * Order Entity is the stored form of an order in the orders table:
     *   PartitionKey is the OrderId, the business identifier used as deduplication key
     *   RowKey is constant, so that (PartitionKey, RowKey) is unique per order
     *   https://learn.microsoft.com/en-us/rest/api/storageservices/Designing-a-Scalable-Partitioning-Strategy-for-Azure-Table-Storage#yyy
     */
    public class OrderEntity : ITableEntity
    {
        public const string OrderRowKey = "Order";

        public string PartitionKey { get; set; } = default!;
        public string RowKey { get; set; } = OrderRowKey;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string CustomerEmail { get; set; } = default!;

        /*
         * WARNING: Table storage has no decimal type, the total is stored as a double for brevity.
         * A double can't represent most decimal amounts exactly, rounding errors add up in sums and comparisons.
         * Prefer storing amounts in minor units (cents) as an Int64, or as a string
         * https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/floating-point-numeric-types
         */
        public double Total { get; set; }

        public DateTime PlacedAt { get; set; }

        public static OrderEntity FromOrderEvent(OrderPlacedEvent order)
        {
            return new OrderEntity
            {
                PartitionKey = order.OrderId,
                CustomerEmail = order.CustomerEmail,
                Total = (double)order.Total,
                PlacedAt = order.PlacedAt
            };
        }
    }
}
