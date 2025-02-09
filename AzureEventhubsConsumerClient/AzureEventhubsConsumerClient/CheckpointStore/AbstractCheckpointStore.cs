namespace AzureEventhubsConsumerClient.CheckpointStore
{
    public abstract class AbstractCheckpointStore
    {
        public string Eventhub { get; set; }

        public string ConsumerGroup { get; set; }

        public string PartitionId { get; set; }

        protected AbstractCheckpointStore(string eventhub,
            string consumerGroup, string partitionId)
        {
            Eventhub = eventhub;
            ConsumerGroup = consumerGroup;
            PartitionId = partitionId;
        }

        public abstract Task<long> GetSequenceNumberAsync();
        public abstract Task SetSequenceNumberAsync(long sequenceNumber);
    }
}
