// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common;
using Epam.Kafka.PubSub.Common.HealthChecks;
using Epam.Kafka.PubSub.Common.Pipeline;
using Epam.Kafka.PubSub.Subscription.Options;
using Epam.Kafka.PubSub.Subscription.Pipeline;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Epam.Kafka.PubSub.Subscription.HealthChecks;

internal class SubscriptionHealthCheck : PubSubHealthCheck
{
    private readonly SubscriptionMonitor _monitor;

    public SubscriptionHealthCheck(IOptionsMonitor<SubscriptionOptions> optionsMonitor, PubSubContext context,
        string name)
        : this(
            (optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor))).Get(
                name ?? throw new ArgumentNullException(nameof(name))),
            (context ?? throw new ArgumentNullException(nameof(context))).Subscriptions.TryGetValue(name,
                out SubscriptionMonitor? monitor)
                ? monitor
                : throw new ArgumentOutOfRangeException(nameof(name),
                    $"Subscription with key {name} not presented in the context"))
    {
    }

    public SubscriptionHealthCheck(SubscriptionOptions options, SubscriptionMonitor monitor)
        : base(options)
    {
        this._monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    }

    protected override PipelineMonitor GetPipelineMonitor()
    {
        return this._monitor;
    }

    protected override HealthStatus GetBatchStatus()
    {
        TimeSpan handlerTimespan = this.UtcNow - this._monitor.Batch.TimestampUtc;

        if (this._monitor.Batch.Value is BatchStatus.None
            or BatchStatus.Reading
            or BatchStatus.Finished
            or BatchStatus.Commiting)
        {
            TimeSpan batchTimespan = this.UtcNow - this._monitor.Result.TimestampUtc;

            if (handlerTimespan > this.Options.HealthChecksThresholdBatch)
            {
                return HealthStatus.Unhealthy;
            }

            if (this._monitor.Result.Value == SubscriptionBatchResult.NotAssigned &&
                batchTimespan > this.Options.HealthChecksThresholdBatch)
            {
                return HealthStatus.Degraded;
            }
        }

        if (this._monitor.Batch.Value == BatchStatus.Running)
        {
            if (handlerTimespan > this.Options.HandlerTimeout)
            {
                return HealthStatus.Unhealthy;
            }
        }

        if (this._monitor.Batch.Value == BatchStatus.Queued)
        {
            if (this.QueuedHealthStatus(handlerTimespan, out HealthStatus healthStatus)) return healthStatus;
        }

        if (this._monitor.Result.Value == SubscriptionBatchResult.Error)
        {
            return HealthStatus.Degraded;
        }

        return HealthStatus.Healthy;
    }
}