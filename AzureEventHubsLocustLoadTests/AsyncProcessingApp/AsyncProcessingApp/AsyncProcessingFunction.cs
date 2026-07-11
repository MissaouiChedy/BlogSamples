using Azure.Storage.Blobs;
using AsyncProcessingApp.Models;
using Azure.Messaging.EventHubs;
using Microsoft.Extensions.Logging;
using AsyncProcessingApp.Observability;
using Microsoft.Azure.Functions.Worker;

namespace AsyncProcessingApp;

public partial class AsyncProcessingFunction(
    ILogger<AsyncProcessingFunction> logger,
    BlobContainerClient containerClient,
    MetricsTracker metricsTracker)
{
    [Function(nameof(AsyncProcessingFunction))]
    public async Task Run(
        [EventHubTrigger(eventHubName: "main-topic", Connection = "EventHubConnection", ConsumerGroup = "main-consumer")]
        EventData[] events)
    {
        /*
         * Batch Telemetry Initialization
         */

        var batchStatistics = new BatchProcessingStatistics(events.Length);
        batchStatistics.Start();

        metricsTracker.TrackBatchSize(batchStatistics.BatchSize);

        foreach (var @event in events)
        {
            /*
             * Event Telemetry Initialization
             */

            EventProcessingStatistics eventStatistics = new(@event);

            eventStatistics.Start();

            metricsTracker
                .TrackQueueLagMilliseconds(eventStatistics.GetQueueLagMilliseconds());

            /*
             * Main Processing Logic: Deserialize the event, enrich it, and store it in Azure Blob Storage.
             */

            MainTopicEvent mainTopicEvent = EventSerializer
                .DeserializeMainTopicEvent(@event.EventBody.ToString());

            EnrichedMainTopicEvent enrichedEvent = EventProcessor
                .CreateEnrichedEvent(mainTopicEvent, eventStatistics.EventEnqueuedTime);

            string blobName = $"event-{enrichedEvent.Id}.json";
            string eventJson = EventSerializer.SerializeProcessedEvents(enrichedEvent);

            await containerClient
                .GetBlobClient(blobName)
                .UploadAsync(BinaryData.FromString(eventJson), overwrite: true);

            /*
             * Event Telemetry Finalization
             */
            eventStatistics.Finish();

            LogEventProcessedSuccessfully(logger, enrichedEvent.Id);

            metricsTracker.TrackCycleTimeMilliseconds(
                eventStatistics.GetCycleTimeMilliseconds(),
                @event.PartitionKey ?? MainTopicEvent.DefaultAreaCode);

            metricsTracker.TrackLeadTimeMilliseconds(
                eventStatistics.GetLeadTimeMilliseconds(),
                @event.PartitionKey ?? MainTopicEvent.DefaultAreaCode);
        }

        /*
         * Batch Telemetry Finalization
         */

        batchStatistics.Finish();

        var batchDurationMs = batchStatistics.GetBatchDurationMilliseconds();

        metricsTracker.TrackBatchDurationMilliseconds(batchDurationMs);

        LogBatchProcessed(
            logger,
            events.Length,
            batchDurationMs);
    }

    /*
     * Log messages are defined as partial methods allowing for compile-time generation of efficient logging code.
     * https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/high-performance-logging#define-logger-messages-with-source-generation
     */

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Processed event {EventId} successfully.")]
    private static partial void LogEventProcessedSuccessfully(ILogger logger, Guid eventId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Processed event batch successfully. Processed {ProcessedCount} events in {DurationMs} ms.")]
    private static partial void LogBatchProcessed(ILogger logger, int processedCount, double durationMs);
}
