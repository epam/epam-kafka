// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Metrics;
using Epam.Kafka.PubSub.Publication.HealthChecks;
using Epam.Kafka.PubSub.Publication.Options;
using Epam.Kafka.PubSub.Publication.Pipeline;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Epam.Kafka.PubSub.Publication.Metrics;

internal sealed class PublicationHealthMetrics : PubSubHealthMetrics
{
    private readonly PublicationMonitor _monitor;
    private readonly PublicationOptions _options;

    public PublicationHealthMetrics(PublicationOptions options, PublicationMonitor monitor) : base(monitor)
    {
        this._options = options ?? throw new ArgumentNullException(nameof(options));
        this._monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    }

    protected override IHealthCheck CreateHealthCheck()
    {
        return new PublicationHealthCheck(this._options, this._monitor);
    }
}