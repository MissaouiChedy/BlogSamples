using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace IdempotencyTechnique1
{
    /*
     * Event Sender is responsible for:
     *   Sending random orders, assigning the MessageId used as deduplication key
     *   Resending already sent orders, simulating producer retries
     */
    public class EventSender(EventHubProducerClient eventHubProducerClient, string[] customerEmails) : IAsyncDisposable
    {

        /*
         * Messages sent during this run, kept to be resent as duplicates
         */
        private readonly List<SentMessage> _sentMessages = [];

        /*
         * Send a random order, the producer assigns the MessageId used as deduplication key
         */
        public async Task SendRandomOrderAsync()
        {
            OrderPlaced order = new(
                $"ORD-{Guid.NewGuid().ToString().Split('-')[0]}",
                customerEmails[Random.Shared.Next(customerEmails.Length)],
                Math.Round((decimal)(Random.Shared.NextDouble() * 500) + 5, 2),
                DateTime.UtcNow);

            /*
             * MessageId is assigned once per logical event, before sending,
             * so that SDK send retries carry the same value
             */
            EventData eventData = new(BinaryData.FromObjectAsJson(order))
            {
                MessageId = Guid.NewGuid().ToString()
            };

            await eventHubProducerClient.SendAsync([eventData], CreateSendOptions(order));
            _sentMessages.Add(new SentMessage(order, eventData.MessageId));

            Console.WriteLine($"SENDER: Sent order {order.OrderId} for {order.CustomerEmail} (MessageId: {eventData.MessageId})");
        }

        /*
         * Resend a random already sent order with the same body and MessageId,
         * simulating a producer retry after a lost acknowledgment: Event Hubs stores
         * a second event with its own sequence number, carrying the same MessageId
         */
        public async Task ResendRandomOrderAsync()
        {
            if (_sentMessages.Count == 0)
            {
                Console.WriteLine("SENDER: No order sent yet in this run, press 's' first");
                return;
            }

            SentMessage sentMessage = _sentMessages[Random.Shared.Next(_sentMessages.Count)];

            EventData eventData = new(BinaryData.FromObjectAsJson(sentMessage.Order))
            {
                MessageId = sentMessage.MessageId
            };

            await eventHubProducerClient.SendAsync([eventData], CreateSendOptions(sentMessage.Order));

            Console.WriteLine($"SENDER: Resent order {sentMessage.Order.OrderId} for {sentMessage.Order.CustomerEmail} (MessageId: {eventData.MessageId})");
        }

        private static SendEventOptions CreateSendOptions(OrderPlaced order)
        {
            return new SendEventOptions { PartitionKey = order.OrderId };
        }

        public ValueTask DisposeAsync()
        {
            return eventHubProducerClient.DisposeAsync();
        }
    }

    public record SentMessage(OrderPlaced Order, string MessageId);
}
