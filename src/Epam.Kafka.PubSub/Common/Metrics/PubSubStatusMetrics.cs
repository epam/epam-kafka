// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Pipeline;

namespace Epam.Kafka.PubSub.Common.Metrics;

internal abstract class PubSubStatusMetrics : MetricsWithName
{
    protected PubSubStatusMetrics(PipelineMonitor monitor) : base(PipelineMonitor.StatusMeterName, monitor)
    {
        this.CreateObservableGauge(PipelineMonitor.StatusPipelineGaugeName, () => (int)monitor.Pipeline.Value,
            "Pipeline processing state.");

        this.CreateObservableGauge(PipelineMonitor.StatusBatchGaugeName, () => (int)monitor.Batch.Value,
            "Batch processing state.");

        this.CreateObservableGauge(PipelineMonitor.StatusBatchAgeGaugeName,
            () => (long)(DateTime.UtcNow - monitor.Batch.TimestampUtc).TotalSeconds,
            "Batch processing state age (seconds).", "seconds");
    }
}