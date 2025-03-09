namespace AzureEventHubsPartitionReceiver
{
    public record EventPayload(Guid Id, string LocationId, string Content);
}
