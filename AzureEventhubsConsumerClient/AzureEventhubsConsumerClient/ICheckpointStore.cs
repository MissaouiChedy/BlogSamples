using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureEventhubsConsumerClient
{

    public class Checkpoint
    {
        public string EventhubNamespace { get; set; }
        public string Eventhub { get; set; }
        public string ConsumerGroup { get; set; }
        public string PartitionId { get; set; }
        public long SequenceNumber { get; set; }
    }

    public interface ICheckpointStore
    {
        Task<Checkpoint> GetCheckpointAsync(string partitionId);
        Task UpdateCheckpointAsync(string partitionId, Checkpoint checkpoint);
    }

    public class RedisCheckpointStore : ICheckpointStore
    {
        public Task<Checkpoint> GetCheckpointAsync(string partitionId)
        {
            throw new NotImplementedException();
        }

        public Task UpdateCheckpointAsync(string partitionId, Checkpoint checkpoint)
        {
            throw new NotImplementedException();
        }
    }
}
