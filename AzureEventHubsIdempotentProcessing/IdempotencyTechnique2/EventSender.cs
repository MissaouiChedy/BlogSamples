using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace IdempotencyTechnique2
{
    /*
     * Event Sender is responsible for:
     *   Sending random orders
     *   Resending already sent orders, simulating producer retries
     *
     * Unlike technique 1, no MessageId is assigned: the OrderId in the event body
     * is the deduplication key, the consumer doesn't rely on any event metadata
     */
    public class EventSender(EventHubProducerClient eventHubProducerClient, string[] customerEmails) : IAsyncDisposable
    {

        /*
         * Orders sent during this run, kept to be resent as duplicates
         */
        private readonly List<OrderPlacedEvent> _sentOrders = [];

        /*
         * Send a random order, the OrderId is assigned once by the business when the order is placed.
         * The OrderId is the deduplication key and must be unique per order: two GUID blocks (48 random bits)
         * keep it short while making a collision in the persistent orders table unlikely
         */
        public async Task SendRandomOrderAsync()
        {
            string[] guidBlocks = Guid.NewGuid().ToString().Split('-');

            OrderPlacedEvent order = new(
                $"ORD-{guidBlocks[0]}-{guidBlocks[1]}",
                customerEmails[Random.Shared.Next(customerEmails.Length)],
                Math.Round((decimal)(Random.Shared.NextDouble() * 500) + 5, 2),
                DateTime.UtcNow);

            await SendOrderAsync(order);
            _sentOrders.Add(order);

            Console.WriteLine($"SENDER: Sent order {order.OrderId} for {order.CustomerEmail}");
        }

        /*
         * Resend a random already sent order with the same body, simulating a producer
         * retry after a lost acknowledgment: Event Hubs stores a second event with its
         * own sequence number, carrying the same OrderId
         */
        public async Task ResendRandomOrderAsync()
        {
            if (_sentOrders.Count == 0)
            {
                Console.WriteLine("SENDER: No order sent yet in this run, press 's' first");
                return;
            }

            OrderPlacedEvent order = _sentOrders[Random.Shared.Next(_sentOrders.Count)];

            await SendOrderAsync(order);

            Console.WriteLine($"SENDER: Resent order {order.OrderId} for {order.CustomerEmail}");
        }

        private Task SendOrderAsync(OrderPlacedEvent order)
        {
            EventData eventData = new(BinaryData.FromObjectAsJson(order));

            return eventHubProducerClient.SendAsync([eventData], new SendEventOptions { PartitionKey = order.OrderId });
        }

        public ValueTask DisposeAsync()
        {
            return eventHubProducerClient.DisposeAsync();
        }
    }
}
