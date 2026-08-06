using AsyncProcessingApp.Models;
using Microsoft.ApplicationInsights;

namespace AsyncProcessingApp.Observability;

/*
 * MetricsTracker is responsible for defining and tracking custom metrics.
 */
public class MetricsTracker(TelemetryClient telemetryClient)
{
    public const string BatchSizeMetricName = "AsyncProcessingApp.BatchSize";
    public const string QueueLagMetricName = "AsyncProcessingApp.QueueLagMilliseconds";
    public const string EventLeadTimeMetricName = "AsyncProcessingApp.EventLeadTimeMilliseconds";
    public const string EventCycleTimeMetricName = "AsyncProcessingApp.EventCycleTimeMilliseconds";
    public const string BatchDurationMetricName = "AsyncProcessingApp.BatchDurationMilliseconds";
    public const string AreaCodeDimensionName = nameof(MainTopicEvent.AreaCode);

    private readonly Metric _batchSizeMetric = telemetryClient.GetMetric(BatchSizeMetricName);
    private readonly Metric _queueLagMetric = telemetryClient.GetMetric(QueueLagMetricName);
    private readonly Metric _eventLeadTimeMetric = telemetryClient.GetMetric(EventLeadTimeMetricName, AreaCodeDimensionName);
    private readonly Metric _eventCycleTimeMetric = telemetryClient.GetMetric(EventCycleTimeMetricName, AreaCodeDimensionName);
    private readonly Metric _batchDurationMetric = telemetryClient.GetMetric(BatchDurationMetricName);

    public void TrackBatchSize(int batchSize)
    {
        _batchSizeMetric.TrackValue(batchSize);
    }

    public void TrackQueueLagMilliseconds(double queueLagMilliseconds)
    {
        /*
         * To avoid clock skew issues, we only track queue lag if the value is non-negative.
         */
        if (queueLagMilliseconds >= 0)
        {
            _queueLagMetric.TrackValue(queueLagMilliseconds);
        }
    }

    public void TrackLeadTimeMilliseconds(double leadTimeMilliseconds, string areaCode)
    {
        _eventLeadTimeMetric.TrackValue(leadTimeMilliseconds, areaCode);
    }

    public void TrackCycleTimeMilliseconds(double cycleTimeMilliseconds, string areaCode)
    {
        _eventCycleTimeMetric.TrackValue(cycleTimeMilliseconds, areaCode);
    }

    public void TrackBatchDurationMilliseconds(double batchDurationMilliseconds)
    {
        _batchDurationMetric.TrackValue(batchDurationMilliseconds);
    }
}
