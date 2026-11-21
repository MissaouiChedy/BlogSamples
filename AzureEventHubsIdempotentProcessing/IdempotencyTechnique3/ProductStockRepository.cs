using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;

namespace IdempotencyTechnique3
{
    /*
     * Product Stock Repository holds the natural state of the consumer: the stock of each product, in Azure Table storage.
     * Each product remembers the Lsn of the last adjustment applied to it:
     *   An adjustment with an Lsn lower or equal to the stored one is a duplicate (or a stale event) and is skipped
     *   The stock and the Lsn are written in the same operation, there is no failure window in between
     *   The write is conditional on the ETag read, so a concurrent writer can't apply the same adjustment twice
     */
    public class ProductStockRepository(TableClient tableClient)
    {
        /*
         * The retry only matters with two consumers writing the same product concurrently, during a
         * partition ownership change (a consumer that lost the partition still processing an old event).
         * The sample runs a single consumer and doesn't demonstrate this case: the adjustments of a product
         * land in the same Event Hubs partition, whose events are handled sequentially, so it never retries.
         * A failed conditional write means another writer won, the next attempt re-reads the product and
         * re-checks the Lsn (skip if already applied, apply on the fresh stock otherwise).
         * The bound prevents an endless loop on a product that keeps changing
         */
        private const int MaxAttempts = 5;

        public async Task<StockAdjustmentResult> TryApplyAdjustmentAsync(StockAdjustedEvent adjustment)
        {
            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                NullableResponse<ProductStockEntity> response = await tableClient
                    .GetEntityIfExistsAsync<ProductStockEntity>(adjustment.Sku, ProductStockEntity.StockRowKey);

                ProductStockEntity? stock = response.HasValue ? response.Value : null;

                /*
                 * This is the deduplication check: the stored Lsn tells where the consumer is at for this product
                 */
                if (stock is not null && adjustment.Lsn <= stock.LastAppliedLsn)
                {
                    return new StockAdjustmentResult(false, stock.QuantityOnHand, stock.LastAppliedLsn);
                }

                try
                {
                    if (stock is null)
                    {
                        stock = ProductStockEntity.FromFirstAdjustment(adjustment);

                        /*
                         * Fails with 409 Conflict when another writer created the product in between
                         */
                        await tableClient.AddEntityAsync(stock);
                    }
                    else
                    {
                        stock.Apply(adjustment);

                        /*
                         * Fails with 412 Precondition Failed when another writer updated the product
                         * since it was read: the check above and this write behave as one atomic step.
                         * This is the fencing: a consumer that lost the partition ownership and still
                         * processes an old event can't overwrite the progress of the new owner
                         */
                        await tableClient.UpdateEntityAsync(stock, stock.ETag, TableUpdateMode.Replace);
                    }

                    return new StockAdjustmentResult(true, stock.QuantityOnHand, stock.LastAppliedLsn);
                }
                catch (RequestFailedException ex) when (IsConcurrentWrite(ex))
                {
                    /*
                     * Another writer won the race, which only happens with several consumers writing the same
                     * product during a partition ownership change (not demonstrated by the sample): read the
                     * product again and re-evaluate the Lsn, if it applied this same adjustment, the next attempt skips it
                     */
                    Console.WriteLine($"RECEIVER: ==> Concurrent write on {adjustment.Sku}, retrying (attempt {attempt})");
                }
            }

            throw new InvalidOperationException($"Could not apply adjustment {adjustment.Lsn} on {adjustment.Sku} after {MaxAttempts} attempts");
        }

        private static bool IsConcurrentWrite(RequestFailedException ex)
        {
            return (ex.Status == 409 && ex.ErrorCode == TableErrorCode.EntityAlreadyExists)
                || (ex.Status == 412 && ex.ErrorCode == TableErrorCode.UpdateConditionNotSatisfied);
        }
    }

    public record StockAdjustmentResult(bool Applied, int QuantityOnHand, long LastAppliedLsn);
}
