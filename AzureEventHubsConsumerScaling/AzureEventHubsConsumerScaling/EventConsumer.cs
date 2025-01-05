using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using System.Text.Json;

namespace AzureEventHubsConsumerScaling
{
    /*
     * Event Consumer is responsible for:
     *   Starting and Stoping Events comsumption
     *   Tracking Partition Id and TimeStamp for Each Message
     *   Providing Processed Messages with Metadata When stopped
     */
    public class EventConsumer
    {
        private readonly EventProcessorClient _eventProcessorClient;
        private readonly int _id;
        private readonly List<MessageReceived> _receivedMessages = new List<MessageReceived>();
        
        public int Id { get => _id; }
        
        public EventConsumer(int id, EventProcessorClient eventProcessorClient)
        {
            _id = id;
            _eventProcessorClient = eventProcessorClient;

            _eventProcessorClient.ProcessEventAsync += ProcessEventHandler;
            _eventProcessorClient.ProcessErrorAsync += ProcessErrorHandler;
        }

        public Task StartConsumptionAsync()
        {
            return _eventProcessorClient.StartProcessingAsync();
        }

        public async Task<List<MessageReceived>> StopConsumptionAsync()
        {
            await _eventProcessorClient.StopProcessingAsync();
            return _receivedMessages;
        }

        protected Task ProcessEventHandler(ProcessEventArgs eventArgs)
        {
            var readOnlySpan = new ReadOnlySpan<byte>(eventArgs.Data.Body.ToArray());
            Message receivedMessage = JsonSerializer
                .Deserialize<Message>(readOnlySpan)!;

            Console.WriteLine($"Consumer {_id} received '{receivedMessage}' from partition {eventArgs.Partition.PartitionId}");
            _receivedMessages.Add(new(eventArgs.Partition.PartitionId, DateTime.UtcNow, receivedMessage));
            return eventArgs.UpdateCheckpointAsync();
        }

        protected Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            Console.WriteLine(@$"\tPartition '{eventArgs.PartitionId}':
    an unhandled exception was encountered. This was not expected to happen.");
            Console.WriteLine(eventArgs.Exception.Message);
            return Task.CompletedTask;
        }
    }

    public record Message(Guid Id, string LocationId, string Content);
    public record MessageReceived(string PartitionId, DateTime Timestamp, Message Message);
}
