
namespace AzureEventHubsEventProcessor
{
    public record EventPayload(Guid Id, string LocationId, string Content);
}
