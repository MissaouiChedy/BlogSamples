namespace AsyncProcessingApp.Models;

public sealed class EnrichedMainTopicEvent : MainTopicEvent
{
    public DateTimeOffset EventEnqueuedAt { get; set; }

    public long ComputationResult { get; set; }

    public DateTimeOffset ProcessedAt { get; set; }
}
