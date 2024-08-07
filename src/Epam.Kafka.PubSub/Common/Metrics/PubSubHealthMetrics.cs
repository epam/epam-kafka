﻿// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Pipeline;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Epam.Kafka.PubSub.Common.Metrics;

internal abstract class PubSubHealthMetrics : MetricsWithName
{
    private static readonly HealthCheckContext Context = new();

    protected PubSubHealthMetrics(PipelineMonitor monitor) : base(PipelineMonitor.HealthMeterName, monitor)
    {
        this.CreateObservableGauge(PipelineMonitor.HealthGaugeName,
            () => (int)this.CreateHealthCheck().CheckHealthAsync(Context).GetAwaiter().GetResult().Status,
            "Health check status: 0 - Unhealthy, 1 - Degraded, 2 - Healthy");
    }

    protected abstract IHealthCheck CreateHealthCheck();
}