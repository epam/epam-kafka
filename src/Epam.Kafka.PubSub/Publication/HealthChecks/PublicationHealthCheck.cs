// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common;
using Epam.Kafka.PubSub.Common.HealthChecks;
using Epam.Kafka.PubSub.Common.Pipeline;
using Epam.Kafka.PubSub.Publication.Options;
using Epam.Kafka.PubSub.Publication.Pipeline;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Epam.Kafka.PubSub.Publication.HealthChecks;

internal class PublicationHealthCheck : PubSubHealthCheck
{
    private readonly PublicationMonitor _monitor;

    public PublicationHealthCheck(IOptionsMonitor<PublicationOptions> optionsMonitor, PubSubContext context,
        string name)
        : this(
            (optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor))).Get(
                name ?? throw new ArgumentNullException(nameof(name))),
            (context ?? throw new ArgumentNullException(nameof(context))).Publications.TryGetValue(name,
                out PublicationMonitor? monitor)
                ? monitor
                : throw new ArgumentOutOfRangeException(nameof(name),
                    $"Publication with key {name} not presented in the context"))
    {
    }

    public PublicationHealthCheck(PublicationOptions options, PublicationMonitor monitor)
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

        if (this._monitor.Batch.Value is BatchStatus.None or BatchStatus.Finished)
        {
            if (handlerTimespan > this.Options.HealthChecksThresholdBatch)
            {
                return HealthStatus.Unhealthy;
            }
        }

        if (this._monitor.Batch.Value is BatchStatus.Running or BatchStatus.Reading or BatchStatus.Commiting)
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

        if (this._monitor.Result.Value is PublicationBatchResult.Error or PublicationBatchResult.ProcessedPartial)
        {
            return HealthStatus.Degraded;
        }

        return HealthStatus.Healthy;
    }
}