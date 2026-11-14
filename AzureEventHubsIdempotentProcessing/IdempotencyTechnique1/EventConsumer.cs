using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using System.Collections.Concurrent;

namespace IdempotencyTechnique1
{
    /*
     * Event Consumer is responsible for:
     *   Starting and Stopping Events consumption
     *   Sending an order confirmation email for each received order
     *   Recording the MessageId of processed events in the processed events store
     */
    public class EventConsumer
    {
        private readonly EventProcessorClient _eventProcessorClient;
        private readonly ProcessedEventsStore _processedEventsStore;
        private readonly FakeEmailSender _emailSender;

        /*
         * Checkpoint every _checkpointThreshold events per partition: events processed
         * since the last checkpoint are replayed after a restart
         */
        private readonly int _checkpointThreshold;
        private readonly ConcurrentDictionary<string, int> _checkpointCounters = new();

        public EventConsumer(
            EventProcessorClient eventProcessorClient,
            ProcessedEventsStore processedEventsStore,
            FakeEmailSender emailSender,
            int checkpointThreshold)
        {
            _eventProcessorClient = eventProcessorClient;
            _processedEventsStore = processedEventsStore;
            _emailSender = emailSender;
            _checkpointThreshold = checkpointThreshold;

            _eventProcessorClient.ProcessEventAsync += ProcessEventHandler;
            _eventProcessorClient.ProcessErrorAsync += ProcessErrorHandler;
        }

        public Task StartConsumptionAsync()
        {
            return _eventProcessorClient.StartProcessingAsync();
        }

        public Task StopConsumptionAsync()
        {
            return _eventProcessorClient.StopProcessingAsync();
        }

        /*
         * Define the event processing method
         */
        protected async Task ProcessEventHandler(ProcessEventArgs eventArgs)
        {
            string partitionId = eventArgs.Partition.PartitionId;
            string? messageId = eventArgs.Data.MessageId;

            Console.WriteLine($"RECEIVER: Received event from partition {partitionId}, with (SeqNum: {eventArgs.Data.SequenceNumber}, MessageId: {messageId})");

            try
            {
                if (await _processedEventsStore.IsProcessedAsync(messageId))
                {
                    Console.WriteLine("RECEIVER: ==> Already processed, skipped");
                }
                else
                {
                    OrderPlaced order = eventArgs.Data.EventBody.ToObjectFromJson<OrderPlaced>()!;

                    await _emailSender.SendOrderConfirmationAsync(order);

                    /*
                     * Failure window: a crash here, after the email is sent and before
                     * the MessageId is recorded, sends a duplicate email on replay
                     */
                    await _processedEventsStore.MarkAsProcessedAsync(messageId);
                }
            }
            catch (Exception ex)
            {
                /*
                 * For brevity, failed events are logged and not retried
                 */
                Console.WriteLine($"RECEIVER: Processing failed: {ex.Message}");
            }

            await CheckpointIfThresholdReachedAsync(eventArgs, partitionId);
        }

        /*
         * Update the checkpoint store every _checkpointThreshold events of the partition,
         * handlers of a given partition are invoked sequentially
         */
        private async Task CheckpointIfThresholdReachedAsync(ProcessEventArgs eventArgs, string partitionId)
        {
            int checkpointCounter = _checkpointCounters.AddOrUpdate(partitionId, 1, (_, c) => c + 1);

            if (checkpointCounter >= _checkpointThreshold)
            {
                await eventArgs.UpdateCheckpointAsync();
                _checkpointCounters[partitionId] = 0;
                Console.WriteLine($"RECEIVER: ==> Checkpointed partition {partitionId}");
            }
        }

        /*
         * Define the Error Handler Method
         */
        protected Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            Console.WriteLine($"RECEIVER: Partition '{eventArgs.PartitionId}': an unhandled exception was encountered. This was not expected to happen.");
            Console.WriteLine($"RECEIVER: {eventArgs.Exception.Message}");
            return Task.CompletedTask;
        }
    }
}
