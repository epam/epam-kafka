// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Pipeline;

namespace Epam.Kafka.PubSub.Common.Metrics;

internal sealed class BatchTimingMetrics : MetricsWithName
{
    public BatchTimingMetrics(PipelineMonitor monitor) : base(PipelineMonitor.TimingMeterName, monitor)
    {
    }
}