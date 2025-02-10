using StackExchange.Redis;

namespace AzureEventHubsEventProcessor.CheckpointOwnershipStore
{
    public class RedisCheckpointOwnershipStore : ICheckpointOwnershipStore
    {
        public string EventhubNamespace { get; private set; }
        public string Eventhub { get; private set; }
        public string ConsumerGroup { get; private set; }

        private readonly IDatabase _cache;

        private readonly IServer _server;

        private readonly TimeSpan _ownershipTtl = TimeSpan.FromMinutes(20);

        private readonly TimeSpan _checkpointTtl = TimeSpan.FromDays(10);

        public RedisCheckpointOwnershipStore(
            ConnectionMultiplexer redisConnection,
            string eventHubNamespace,
            string eventhub,
            string consumerGroup)
        {
            _cache = redisConnection.GetDatabase();
            _server = redisConnection
                .GetServer(redisConnection.GetEndPoints().First());

            EventhubNamespace = eventHubNamespace;
            Eventhub = eventhub;
            ConsumerGroup = consumerGroup;
        }

        public async Task<Checkpoint> GetCheckpointAsync(string partitionId)
        {
            var value = await _cache
                .HashGetAllAsync(GetCheckpointKey(partitionId));

            if (value != null
                && value.Length > 0)
            {

                return new Checkpoint(
                    partitionId,
                    long.Parse(value.First(x => x.Name == nameof(Checkpoint.Offset)).Value),
                    long.Parse(value.First(x => x.Name == nameof(Checkpoint.SequenceNumber)).Value),
                    value.First(x => x.Name == nameof(Checkpoint.OwnerId)).Value);
            }

            return Checkpoint.Null;
        }

        public async Task SetCheckpointAsync(string partitionId, Checkpoint checkpoint)
        {
            await _cache.HashSetAsync(GetCheckpointKey(partitionId), new[]
            {
                new HashEntry(nameof(checkpoint.PartitionId), checkpoint.PartitionId),
                new HashEntry(nameof(checkpoint.Offset), checkpoint.Offset),
                new HashEntry(nameof(checkpoint.SequenceNumber), checkpoint.SequenceNumber),
                new HashEntry(nameof(checkpoint.OwnerId), checkpoint.OwnerId)
            });

            await _cache.KeyExpireAsync(GetCheckpointKey(partitionId), _checkpointTtl);
        }

        public async Task<Ownership> GetOwnershipAsync(string partitionId)
        {
            var value = await _cache.HashGetAllAsync(GetOwnershipKey(partitionId));
            
            if (value != null
                && value.Length > 0)
            {
                return new Ownership(
                    value.First(x => x.Name == "PartitionId").Value.ToString(),
                    value.First(x => x.Name == "OwnerId").Value.ToString(),
                    DateTimeOffset.Parse(value.First(x => x.Name == "LastModifiedTime").Value.ToString()),
                    value.First(x => x.Name == "Version").Value.ToString());
            }
            return Ownership.Null;
        }

        public async Task SetOwnershipAsync(string partitionId, Ownership ownership)
        {
            await _cache.HashSetAsync(GetOwnershipKey(partitionId),
                [
                    new HashEntry(nameof(ownership.PartitionId), ownership.PartitionId),
                    new HashEntry(nameof(ownership.OwnerId), ownership.OwnerId),
                    new HashEntry(nameof(ownership.LastModifiedTime), ownership.LastModifiedTime.UtcDateTime.ToString("o")),
                    new HashEntry(nameof(ownership.Version), ownership.Version)
                ]
            );

            await _cache.KeyExpireAsync(GetOwnershipKey(partitionId), _ownershipTtl);
        }

        public async Task<IEnumerable<Checkpoint>> GetAllCheckpointsAsync()
        {
            var keys = _server
                .KeysAsync(_cache.Database, $"{EventhubNamespace}|{Eventhub}|{ConsumerGroup}|chk|*");

            var checkpoints = new List<Checkpoint>();

            await foreach (var key in keys)
            {
                checkpoints.Add(await GetCheckpointAsync(key));
            }

            return checkpoints;
        }

        public async Task<IEnumerable<Ownership>> GetAllOwnershipsAsync()
        {
            var keys = _server
                .KeysAsync(_cache.Database, $"{EventhubNamespace}|{Eventhub}|{ConsumerGroup}|own|*");

            var ownerships = new List<Ownership>();

            await foreach (var key in keys)
            {
                string partitionId = key.ToString().Split('|').Last();
                ownerships.Add(await GetOwnershipAsync(partitionId));
            }

            return ownerships;
        }

        private string GetCheckpointKey(string partitionId)
        {
            return $"{EventhubNamespace}|{Eventhub}|{ConsumerGroup}|chk|{partitionId}";
        }

        private string GetOwnershipKey(string partitionId)
        {
            return $"{EventhubNamespace}|{Eventhub}|{ConsumerGroup}|own|{partitionId}";
        }
    }
}
