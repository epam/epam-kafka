// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Metrics;
using Epam.Kafka.PubSub.Subscription.HealthChecks;
using Epam.Kafka.PubSub.Subscription.Options;
using Epam.Kafka.PubSub.Subscription.Pipeline;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Epam.Kafka.PubSub.Subscription.Metrics;

internal sealed class SubscriptionHealthMetrics : PubSubHealthMetrics
{
    private readonly SubscriptionMonitor _monitor;
    private readonly SubscriptionOptions _options;

    public SubscriptionHealthMetrics(SubscriptionOptions options, SubscriptionMonitor monitor) : base(monitor)
    {
        this._options = options ?? throw new ArgumentNullException(nameof(options));
        this._monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    }

    protected override IHealthCheck CreateHealthCheck()
    {
        return new SubscriptionHealthCheck(this._options, this._monitor);
    }
}