namespace AsyncProcessingApp.Observability;

/*
 * BatchProcessingStatistics is responsible for tracking
 * the processing statistics of a batch of events.
 */
public class BatchProcessingStatistics(int batchSize)
{
    public int BatchSize { get; } = batchSize;
    public DateTimeOffset? BatchStartedAt { get; private set; }
    public DateTimeOffset? BatchEndedAt { get; private set; }

    public void Start() => BatchStartedAt = DateTimeOffset.UtcNow;
    public void Finish() => BatchEndedAt = DateTimeOffset.UtcNow;

    public double GetBatchDurationMilliseconds() =>
        BatchStartedAt is { } batchStartedAt
            && BatchEndedAt is { } batchEndedAt
            ? (batchEndedAt - batchStartedAt).TotalMilliseconds
            : 0;
}
