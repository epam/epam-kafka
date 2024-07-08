// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Metrics;
using Epam.Kafka.PubSub.Common.Pipeline;
using Epam.Kafka.PubSub.Publication.Pipeline;

namespace Epam.Kafka.PubSub.Publication.Metrics;

internal sealed class PublicationStatusMetrics : PubSubStatusMetrics
{
    public PublicationStatusMetrics(PublicationMonitor monitor) : base(monitor)
    {
        this.CreateObservableGauge(PipelineMonitor.StatusResultGaugeName, () => (int)monitor.Result.Value,
            "The result of publication batch iteration: 0 - None, 1 - Error, 2 - Empty (no messages to publish), 3 - Processed, 30 - Processed partially");
    }
}