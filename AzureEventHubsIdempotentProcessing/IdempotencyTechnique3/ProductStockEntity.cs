using Azure;
using Azure.Data.Tables;

namespace IdempotencyTechnique3
{
    /*
     * Product Stock Entity is the stored form of a product stock in the inventory table:
     *   PartitionKey is the Sku of the product
     *   RowKey is constant, so that (PartitionKey, RowKey) is unique per product
     *   LastAppliedLsn and LastAdjustedAt record where the consumer is at for this product,
     *   they are stored in the same entity as the stock, so they change in the same write
     */
    public class ProductStockEntity : ITableEntity
    {
        public const string StockRowKey = "Stock";

        public string PartitionKey { get; set; } = default!;
        public string RowKey { get; set; } = StockRowKey;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public int QuantityOnHand { get; set; }

        /*
         * The Lsn of the last adjustment applied to this product, any adjustment
         * with a lower or equal Lsn is considered already applied
         */
        public long LastAppliedLsn { get; set; }

        public DateTime LastAdjustedAt { get; set; }

        public static ProductStockEntity FromFirstAdjustment(StockAdjustedEvent adjustment)
        {
            ProductStockEntity stock = new() { PartitionKey = adjustment.Sku };
            stock.Apply(adjustment);
            return stock;
        }

        public void Apply(StockAdjustedEvent adjustment)
        {
            QuantityOnHand += adjustment.Delta;
            LastAppliedLsn = adjustment.Lsn;
            LastAdjustedAt = adjustment.AdjustedAt;
        }
    }
}
