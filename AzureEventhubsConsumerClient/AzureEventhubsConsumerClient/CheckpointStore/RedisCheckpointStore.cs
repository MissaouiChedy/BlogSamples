using StackExchange.Redis;

namespace AzureEventhubsConsumerClient.CheckpointStore
{
    public class RedisCheckpointStore : AbstractCheckpointStore
    {
        private readonly IDatabase _cache;
        private readonly long _ttlSeconds = 3600;
        private readonly string _key;

        public RedisCheckpointStore(string eventhub, string consumerGroup,
            string partitionId, ConnectionMultiplexer connectionMultiplexer)
            : base(eventhub, consumerGroup, partitionId)
        {
            _key = $"{Eventhub}|{ConsumerGroup}|{PartitionId}";
            _cache = connectionMultiplexer.GetDatabase();
        }

        public override async Task<long> GetSequenceNumberAsync()
        {
            string? sequenceNumberValue = await _cache.StringGetAsync(_key);

            if (long.TryParse(sequenceNumberValue, out long sequenceNumber))
            {
                return sequenceNumber;
            }

            return -1;
        }

        public async override Task SetSequenceNumberAsync(long sequenceNumber)
        {
            await _cache.StringSetAsync(_key, sequenceNumber);
            await _cache.KeyExpireAsync(_key, TimeSpan.FromSeconds(_ttlSeconds));
        }
    }
}
