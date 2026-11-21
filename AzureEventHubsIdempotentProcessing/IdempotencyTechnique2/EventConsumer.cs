using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using System.Collections.Concurrent;

namespace IdempotencyTechnique2
{
    /*
     * Event Consumer is responsible for:
     *   Starting and Stopping Events consumption
     *   Storing each received order in the orders table, the natural state of the consumer
     *   Skipping orders the table already holds
     */
    public class EventConsumer
    {
        private readonly EventProcessorClient _eventProcessorClient;
        private readonly OrderRepository _orderRepository;

        /*
         * Checkpoint every _checkpointThreshold events per partition: events processed
         * since the last checkpoint are replayed after a restart
         */
        private readonly int _checkpointThreshold;
        private readonly ConcurrentDictionary<string, int> _checkpointCounters = new();

        public EventConsumer(
            EventProcessorClient eventProcessorClient,
            OrderRepository orderRepository,
            int checkpointThreshold)
        {
            _eventProcessorClient = eventProcessorClient;
            _orderRepository = orderRepository;
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

            try
            {
                OrderPlacedEvent order = eventArgs
                    .Data.EventBody.ToObjectFromJson<OrderPlacedEvent>()!;

                Console.WriteLine($"RECEIVER: Received event from partition {partitionId}, with (SeqNum: {eventArgs.Data.SequenceNumber}, OrderId: {order.OrderId})");

                /*
                 * No check before the write: the insert is the check, and it is atomic
                 */
                if (await _orderRepository.TryCreateOrderAsync(order))
                {
                    Console.WriteLine($"RECEIVER: ==> Order {order.OrderId} stored for {order.CustomerEmail}, total {order.Total:F2}");
                }
                else
                {
                    Console.WriteLine($"RECEIVER: ==> Order {order.OrderId} already stored, skipped");
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
