using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace IdempotencyTechnique3
{
    /*
     * Event Sender simulates the inventory system publishing its changes, it is responsible for:
     *   Sending random stock adjustments, assigning the monotonic Lsn used as ordering metadata
     *   Resending already sent adjustments, simulating producer retries
     *   Sending two adjustments out of order, showing the reliance on ordering
     */
    public class EventSender(EventHubProducerClient eventHubProducerClient, string[] skus) : IAsyncDisposable
    {
        /*
         * Adjustments sent during this run, kept to be resent as duplicates
         */
        private readonly List<StockAdjustedEvent> _sentAdjustments = [];

        private long _lastLsn;

        /*
         * Send a random adjustment of a random product
         */
        public async Task SendRandomAdjustmentAsync()
        {
            StockAdjustedEvent adjustment = CreateRandomAdjustment(skus[Random.Shared.Next(skus.Length)]);

            await SendAdjustmentAsync(adjustment);
            _sentAdjustments.Add(adjustment);

            Console.WriteLine($"SENDER: Sent {Describe(adjustment)}");
        }

        /*
         * Resend a random already sent adjustment with the same body, simulating a producer
         * retry after a lost acknowledgment: Event Hubs stores a second event with its
         * own sequence number, carrying the same Lsn
         */
        public async Task ResendRandomAdjustmentAsync()
        {
            if (_sentAdjustments.Count == 0)
            {
                Console.WriteLine("SENDER: No adjustment sent yet in this run, press 's' first");
                return;
            }

            StockAdjustedEvent adjustment = _sentAdjustments[Random.Shared.Next(_sentAdjustments.Count)];

            await SendAdjustmentAsync(adjustment);

            Console.WriteLine($"SENDER: Resent {Describe(adjustment)}");
        }

        /*
         * Create two adjustments of the same product and send the newer one first, simulating
         * a change that reaches the event hub after a newer one (concurrent publishers, or a
         * retry that lands late). The consumer skips the older one as already applied: its
         * delta is lost, this technique relies on the producer publishing changes in order
         */
        public async Task SendOutOfOrderAdjustmentsAsync()
        {
            string sku = skus[Random.Shared.Next(skus.Length)];
            StockAdjustedEvent older = CreateRandomAdjustment(sku);
            StockAdjustedEvent newer = CreateRandomAdjustment(sku);

            await SendAdjustmentAsync(newer);
            Console.WriteLine($"SENDER: Sent newer {Describe(newer)}");

            await SendAdjustmentAsync(older);
            Console.WriteLine($"SENDER: Sent older {Describe(older)} out of order, its delta will be lost");
        }

        private StockAdjustedEvent CreateRandomAdjustment(string sku)
        {
            bool isRestock = Random.Shared.Next(3) == 0;

            return new StockAdjustedEvent(
                sku,
                isRestock ? Random.Shared.Next(10, 51) : -Random.Shared.Next(1, 6),
                isRestock ? "Restock" : "Sale",
                NextLsn(),
                DateTime.UtcNow);
        }

        /*
         * In production the Lsn comes from the source database (the CDC __$start_lsn of the change).
         * Here it is simulated with clock ticks, so that it keeps increasing across runs: the inventory
         * table persists between runs, a counter restarting at 1 would make every new adjustment look
         * already applied. The Lsn must never go backwards, a clock moved back would break this simulation
         */
        private long NextLsn()
        {
            _lastLsn = Math.Max(_lastLsn + 1, DateTime.UtcNow.Ticks);
            return _lastLsn;
        }

        /*
         * The Sku is the partition key: the adjustments of a product land in the same
         * Event Hubs partition and are received in the order they were sent
         */
        private Task SendAdjustmentAsync(StockAdjustedEvent adjustment)
        {
            EventData eventData = new(BinaryData.FromObjectAsJson(adjustment));

            return eventHubProducerClient.SendAsync([eventData], new SendEventOptions { PartitionKey = adjustment.Sku });
        }

        private static string Describe(StockAdjustedEvent adjustment)
        {
            return $"adjustment on {adjustment.Sku} (Reason: {adjustment.Reason}, Delta: {adjustment.Delta:+0;-0}, Lsn: {adjustment.Lsn})";
        }

        public ValueTask DisposeAsync()
        {
            return eventHubProducerClient.DisposeAsync();
        }
    }
}
