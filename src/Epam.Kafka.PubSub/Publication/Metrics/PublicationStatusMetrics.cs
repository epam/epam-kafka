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
            "The result of last batch iteration.");
    }
}