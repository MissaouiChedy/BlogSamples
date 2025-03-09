namespace AzureEventHubsPartitionReceiver.MultipleReceivers
{
    public record EventPayload(Guid Id, string LocationId, string Content);
}
