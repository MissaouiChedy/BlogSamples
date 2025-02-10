using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureEventHubsEventProcessor.CheckpointOwnershipStore
{
    public interface ICheckpointOwnershipStore
    {
        public string EventhubNamespace { get; }
        public string Eventhub { get; }
        public string ConsumerGroup { get; }
        public Task<Checkpoint> GetCheckpointAsync(string partitionId);
        public Task<IEnumerable<Checkpoint>> GetAllCheckpointsAsync();
        public Task SetCheckpointAsync(string partitionId, Checkpoint checkpoint);
        public Task<Ownership> GetOwnershipAsync(string partitionId);
        public Task SetOwnershipAsync(string partitionId, Ownership ownership);
        public Task<IEnumerable<Ownership>> GetAllOwnershipsAsync();
    }
}
