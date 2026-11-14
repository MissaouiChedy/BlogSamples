using StackExchange.Redis;

namespace IdempotencyTechnique1
{
    /*
     * Processed Events Store remembers the MessageId of processed events in Redis:
     *   Keys are scoped by consumer group, since each consumer group is an independent reader
     *   Keys expire after a TTL, which must exceed the worst-case replay window
     */
    public class ProcessedEventsStore
    {
        public string EventhubNamespace { get; private set; }
        public string Eventhub { get; private set; }
        public string ConsumerGroup { get; private set; }

        private readonly IDatabase _cache;

        private readonly TimeSpan _processedEventTtl;

        public ProcessedEventsStore(
            ConnectionMultiplexer redisConnection,
            string eventHubNamespace,
            string eventhub,
            string consumerGroup,
            TimeSpan processedEventTtl)
        {
            _cache = redisConnection.GetDatabase();

            EventhubNamespace = eventHubNamespace;
            Eventhub = eventhub;
            ConsumerGroup = consumerGroup;
            _processedEventTtl = processedEventTtl;
        }

        public async Task<bool> IsProcessedAsync(string messageId)
        {
            return await _cache.KeyExistsAsync(GetProcessedEventKey(messageId));
        }

        /*
         * SET NX is atomic: when two consumers record the same MessageId concurrently,
         * only one succeeds. Returns false when the MessageId was already recorded
         */
        public async Task<bool> MarkAsProcessedAsync(string messageId)
        {
            return await _cache.StringSetAsync(
                GetProcessedEventKey(messageId),
                DateTime.UtcNow.ToString("o"),
                _processedEventTtl,
                When.NotExists);
        }

        private string GetProcessedEventKey(string messageId)
        {
            return $"{EventhubNamespace}|{Eventhub}|{ConsumerGroup}|processed|{messageId}";
        }
    }
}
