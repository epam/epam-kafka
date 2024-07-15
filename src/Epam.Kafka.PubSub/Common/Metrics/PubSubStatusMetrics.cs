// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Pipeline;

namespace Epam.Kafka.PubSub.Common.Metrics;

internal abstract class PubSubStatusMetrics : MetricsWithName
{
    protected PubSubStatusMetrics(PipelineMonitor monitor) : base(PipelineMonitor.StatusMeterName, monitor)
    {
        this.CreateObservableGauge(PipelineMonitor.StatusPipelineGaugeName, () => (int)monitor.Pipeline.Value,
            "Pipeline processing state: 0 - Not started, 1 - Cancelled, 2 - Disabled, 3 - Running, 4 - Not running (will run after timeout), 5 - Not running (only application restart could help)");
    }
}