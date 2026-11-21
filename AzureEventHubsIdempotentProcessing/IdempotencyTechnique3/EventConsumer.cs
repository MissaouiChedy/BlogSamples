using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using System.Collections.Concurrent;

namespace IdempotencyTechnique3
{
    /*
     * Event Consumer is responsible for:
     *   Starting and Stopping Events consumption
     *   Applying each received stock adjustment to the product stock, the natural state of the consumer
     *   Skipping adjustments whose Lsn was already applied to the product
     */
    public class EventConsumer
    {
        private readonly EventProcessorClient _eventProcessorClient;
        private readonly ProductStockRepository _productStockRepository;

        /*
         * Checkpoint every _checkpointThreshold events per partition: events processed
         * since the last checkpoint are replayed after a restart
         */
        private readonly int _checkpointThreshold;
        private readonly ConcurrentDictionary<string, int> _checkpointCounters = new();

        public EventConsumer(
            EventProcessorClient eventProcessorClient,
            ProductStockRepository productStockRepository,
            int checkpointThreshold)
        {
            _eventProcessorClient = eventProcessorClient;
            _productStockRepository = productStockRepository;
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
                StockAdjustedEvent adjustment = eventArgs
                    .Data.EventBody.ToObjectFromJson<StockAdjustedEvent>()!;

                Console.WriteLine($"RECEIVER: Received event from partition {partitionId}, with (SeqNum: {eventArgs.Data.SequenceNumber}, Sku: {adjustment.Sku}, Lsn: {adjustment.Lsn})");

                StockAdjustmentResult result = await _productStockRepository.TryApplyAdjustmentAsync(adjustment);

                if (result.Applied)
                {
                    Console.WriteLine($"RECEIVER: ==> Adjustment applied on {adjustment.Sku} (Reason: {adjustment.Reason}, Delta: {adjustment.Delta:+0;-0}), {result.QuantityOnHand} on hand");
                }
                else
                {
                    Console.WriteLine($"RECEIVER: ==> Lsn {adjustment.Lsn} <= last applied {result.LastAppliedLsn} on {adjustment.Sku}, skipped ({result.QuantityOnHand} on hand)");
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
