using Azure.Messaging.EventHubs;

namespace AsyncProcessingApp.Observability;

/*
 * EventProcessingStatistics is responsible for tracking
 * the processing statistics of an individual event.
 */

public class EventProcessingStatistics(EventData eventData)
{
    private DateTimeOffset? _processingStartedAt;

    private DateTimeOffset? _processingEndedAt;

    public DateTimeOffset EventEnqueuedTime { get; } = eventData.EnqueuedTime;
    public void Start() => _processingStartedAt = DateTimeOffset.UtcNow;
    public void Finish() => _processingEndedAt = DateTimeOffset.UtcNow;

    public double GetCycleTimeMilliseconds() =>
        _processingStartedAt is { } processingStartedAt && _processingEndedAt is { } processingEndedAt
            ? (processingEndedAt - processingStartedAt).TotalMilliseconds
            : 0;

    public double GetLeadTimeMilliseconds() =>
        _processingEndedAt is { } processingEndedAt && EventEnqueuedTime != default
            ? (processingEndedAt - EventEnqueuedTime).TotalMilliseconds
            : 0;

    public double GetQueueLagMilliseconds() =>
        _processingStartedAt is { } processingStartedAt
            ? (processingStartedAt - EventEnqueuedTime).TotalMilliseconds
            : 0;
}
