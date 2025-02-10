using Azure.Messaging.EventHubs.Primitives;

namespace AzureEventHubsEventProcessor
{
    public class  CustomPartitionContext : EventProcessorPartition 
    {
        public string Custom { get; set; } = "";
    }
}
