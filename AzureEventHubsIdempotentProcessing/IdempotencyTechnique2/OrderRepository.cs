using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;

namespace IdempotencyTechnique2
{
    /*
     * Order Repository holds the natural state of the consumer: the orders it created, in Azure Table storage.
     * There is no dedicated deduplication store, the orders table plays that role:
     *   The OrderId is the key of the entity, the table rejects a second insert of the same order
     *   The deduplication key is written in the same operation as the state change, there is no failure window in between
     */
    public class OrderRepository(TableClient tableClient)
    {
        /*
         * Insert the order, returns false when the order is already stored.
         * A duplicate key violation is expected and handled as "already processed",
         * including when two consumers insert the same order concurrently: only one insert succeeds
         */
        public async Task<bool> TryCreateOrderAsync(OrderPlacedEvent orderEvent)
        {
            try
            {
                var orderEntity = OrderEntity.FromOrderEvent(orderEvent);
                await tableClient.AddEntityAsync(orderEntity);
                return true;
            }
            catch (RequestFailedException ex) when (ex.Status == 409 && ex.ErrorCode == TableErrorCode.EntityAlreadyExists)
            {
                /*
                 * The rejected insert is still a billed storage transaction
                 */
                return false;
            }
        }
    }
}
