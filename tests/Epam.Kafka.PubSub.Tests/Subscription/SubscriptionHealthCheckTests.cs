// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Pipeline;
using Epam.Kafka.PubSub.Subscription.HealthChecks;
using Epam.Kafka.PubSub.Subscription.Options;
using Epam.Kafka.PubSub.Subscription.Pipeline;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using Shouldly;

using Xunit;

namespace Epam.Kafka.PubSub.Tests.Subscription;

public class SubscriptionHealthCheckTests
{
    private static readonly HealthCheckContext Context = new();

    [Theory]
    [InlineData(0, PipelineStatus.None, BatchStatus.None, SubscriptionBatchResult.None, 0, HealthStatus.Degraded)]
    [InlineData(0, PipelineStatus.None, BatchStatus.None, SubscriptionBatchResult.None, 600, HealthStatus.Unhealthy)]
    [InlineData(0, PipelineStatus.Failed, BatchStatus.None, SubscriptionBatchResult.None, 0, HealthStatus.Unhealthy)]
    [InlineData(0, PipelineStatus.RetryTimeout, BatchStatus.None, SubscriptionBatchResult.None, 0, HealthStatus.Unhealthy)]
    [InlineData(0, PipelineStatus.Cancelled, BatchStatus.None, SubscriptionBatchResult.None, 0, HealthStatus.Degraded)]
    [InlineData(0, PipelineStatus.Disabled, BatchStatus.None, SubscriptionBatchResult.None, 0, HealthStatus.Healthy)]
    [InlineData(1, PipelineStatus.Running, BatchStatus.None, SubscriptionBatchResult.None, 0, HealthStatus.Unhealthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.None, SubscriptionBatchResult.None, 0, HealthStatus.Healthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Finished, SubscriptionBatchResult.None, 0, HealthStatus.Healthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.None, SubscriptionBatchResult.None, 600, HealthStatus.Unhealthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Finished, SubscriptionBatchResult.None, 600, HealthStatus.Unhealthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Running, SubscriptionBatchResult.None, 0, HealthStatus.Healthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Running, SubscriptionBatchResult.None, 600, HealthStatus.Unhealthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Reading, SubscriptionBatchResult.None, 0, HealthStatus.Healthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Commiting, SubscriptionBatchResult.None, 0, HealthStatus.Healthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Queued, SubscriptionBatchResult.None, 0, HealthStatus.Healthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Commiting, SubscriptionBatchResult.None, 600, HealthStatus.Unhealthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Queued, SubscriptionBatchResult.None, 600, HealthStatus.Unhealthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Finished, SubscriptionBatchResult.Error, 0, HealthStatus.Degraded)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Finished, SubscriptionBatchResult.NotAssigned, 0, HealthStatus.Healthy)]
    public async Task CheckHealth(int pipelineRetry, PipelineStatus p, BatchStatus b, SubscriptionBatchResult r,
        int seconds, HealthStatus expectedStatus)
    {
        SubscriptionMonitor monitor = new("any");

        monitor.Pipeline.Update(p);
        monitor.Batch.Update(b);
        monitor.Result.Update(r);
        monitor.PipelineRetryIteration = pipelineRetry;

        var check = new CheckForTest(monitor, seconds);

        HealthCheckResult result = await check.CheckHealthAsync(Context);
        result.Status.ShouldBe(expectedStatus);
    }

    private class CheckForTest : SubscriptionHealthCheck
    {
        private readonly int _seconds;

        public CheckForTest(SubscriptionMonitor monitor, int seconds) : base(new SubscriptionOptions(), monitor)
        {
            this._seconds = seconds;
        }

        protected override DateTime UtcNow => DateTime.UtcNow.AddSeconds(this._seconds);
    }
}