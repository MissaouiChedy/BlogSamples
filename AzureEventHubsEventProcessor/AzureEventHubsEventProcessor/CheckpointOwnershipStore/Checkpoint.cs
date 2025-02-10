
namespace AzureEventHubsEventProcessor.CheckpointOwnershipStore
{
    public record Checkpoint(string PartitionId, long Offset,
        long SequenceNumber, string OwnerId)
    {
        public static Checkpoint Null { get; } 
            = new Checkpoint(string.Empty, -1, -1, string.Empty);
    }
    public record Ownership(string PartitionId, string OwnerId,
        DateTimeOffset LastModifiedTime, string Version)
    {
        public static Ownership Null { get; } 
            = new Ownership(string.Empty, string.Empty, 
                DateTime.MinValue, string.Empty);
    }
}
