// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Options;
using Epam.Kafka.PubSub.Common.Pipeline;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Epam.Kafka.PubSub.Common.HealthChecks;

internal abstract class PubSubHealthCheck : IHealthCheck
{

    protected PubSubHealthCheck(PubSubOptions options)
    {
        this.Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    protected PubSubOptions Options { get; }
    protected virtual DateTime UtcNow => DateTime.UtcNow;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
    {
        PipelineMonitor monitor = this.GetPipelineMonitor();

        HealthStatus result;

        TimeSpan pipelineTimespan = this.UtcNow - monitor.Pipeline.TimestampUtc;

        switch (monitor.Pipeline.Value)
        {
            case PipelineStatus.None:
                result = pipelineTimespan > this.Options.HealthChecksThresholdPipeline
                    ? HealthStatus.Unhealthy
                    : HealthStatus.Degraded;
                break;
            case PipelineStatus.Failed:
            case PipelineStatus.RetryTimeout:
                result = HealthStatus.Unhealthy;
                break;
            case PipelineStatus.Cancelled:
                result = HealthStatus.Degraded;
                break;
            case PipelineStatus.Disabled:
                result = HealthStatus.Healthy;
                break;
            case PipelineStatus.Running:
                {
                    result = monitor.PipelineRetryIteration > 0 ? HealthStatus.Unhealthy : this.GetBatchStatus();

                    break;
                }
            default:
                throw new InvalidOperationException(
                    $"Unknown pipeline status {monitor.Pipeline.Value}. Supported values: " +
                    string.Join(", ", Enum.GetNames(typeof(PipelineStatus))));
        }

        return Task.FromResult(new HealthCheckResult(
            result == HealthStatus.Unhealthy ? context.Registration?.FailureStatus ?? HealthStatus.Unhealthy : result,
            monitor.ToString()));
    }

    protected abstract PipelineMonitor GetPipelineMonitor();

    protected abstract HealthStatus GetBatchStatus();

    protected bool QueuedHealthStatus(TimeSpan handlerTimespan, out HealthStatus healthStatus)
    {
        if (this.Options.HandlerConcurrencyGroup.HasValue)
        {
            if (handlerTimespan > (this.Options.HealthChecksMaxQueued ?? this.Options.HandlerTimeout))
            {
                {
                    healthStatus = HealthStatus.Degraded;
                    return true;
                }
            }
        }
        else if (handlerTimespan > this.Options.HealthChecksThresholdBatch)
        {
            {
                healthStatus = HealthStatus.Unhealthy;
                return true;
            }
        }

        healthStatus = HealthStatus.Healthy;
        return false;
    }
}