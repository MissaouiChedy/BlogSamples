namespace IdempotencyTechnique3
{
    /*
     * A stock adjustment of a single product, emitted by the inventory system:
     *   Delta is a relative change (a restock adds units, a sale removes units), applying it twice corrupts the stock
     *   Lsn is the ordering metadata supplied by the producer, it stands in for the
     *   SQL Server CDC log sequence number (__$start_lsn) of the change
     *   https://learn.microsoft.com/en-us/sql/relational-databases/system-tables/cdc-capture-instance-ct-transact-sql
     *
     * The event is about one entity (the Sku), so that one ordering value per entity is enough
     */
    public record StockAdjustedEvent(
        string Sku, int Delta, string Reason, long Lsn, DateTime AdjustedAt);
}
