﻿// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Metrics;
using Epam.Kafka.PubSub.Common.Pipeline;
using Epam.Kafka.PubSub.Subscription.Pipeline;

namespace Epam.Kafka.PubSub.Subscription.Metrics;

internal sealed class SubscriptionStatusMetrics : PubSubStatusMetrics
{
    public SubscriptionStatusMetrics(SubscriptionMonitor monitor) : base(monitor)
    {
        this.CreateObservableGauge(PipelineMonitor.StatusResultGaugeName, () => (int)monitor.Result.Value,
            "The result of last batch iteration.");
    }
}