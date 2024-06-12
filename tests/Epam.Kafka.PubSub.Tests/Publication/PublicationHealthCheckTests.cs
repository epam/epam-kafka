// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common;
using Epam.Kafka.PubSub.Common.Pipeline;
using Epam.Kafka.PubSub.Publication.HealthChecks;
using Epam.Kafka.PubSub.Publication.Options;
using Epam.Kafka.PubSub.Publication.Pipeline;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using Shouldly;

using Xunit;

namespace Epam.Kafka.PubSub.Tests.Publication;

public class PublicationHealthCheckTests
{
    private static readonly HealthCheckContext Context = new();

    [Theory]
    [InlineData(0, PipelineStatus.None, BatchStatus.None, PublicationBatchResult.None, 0, HealthStatus.Degraded)]
    [InlineData(0, PipelineStatus.None, BatchStatus.None, PublicationBatchResult.None, 600, HealthStatus.Unhealthy)]
    [InlineData(0, PipelineStatus.Failed, BatchStatus.None, PublicationBatchResult.None, 0, HealthStatus.Unhealthy)]
    [InlineData(0, PipelineStatus.RetryTimeout, BatchStatus.None, PublicationBatchResult.None, 0, HealthStatus.Unhealthy)]
    [InlineData(0, PipelineStatus.Cancelled, BatchStatus.None, PublicationBatchResult.None, 0, HealthStatus.Degraded)]
    [InlineData(0, PipelineStatus.Disabled, BatchStatus.None, PublicationBatchResult.None, 0, HealthStatus.Healthy)]
    [InlineData(1, PipelineStatus.Running, BatchStatus.None, PublicationBatchResult.None, 0, HealthStatus.Unhealthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.None, PublicationBatchResult.None, 0, HealthStatus.Healthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Finished, PublicationBatchResult.None, 0, HealthStatus.Healthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.None, PublicationBatchResult.None, 600, HealthStatus.Unhealthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Finished, PublicationBatchResult.None, 600, HealthStatus.Unhealthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Running, PublicationBatchResult.None, 0, HealthStatus.Healthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Reading, PublicationBatchResult.None, 0, HealthStatus.Healthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Commiting, PublicationBatchResult.None, 0, HealthStatus.Healthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Queued, PublicationBatchResult.None, 0, HealthStatus.Healthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Commiting, PublicationBatchResult.None, 600, HealthStatus.Unhealthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Queued, PublicationBatchResult.None, 600, HealthStatus.Unhealthy)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Finished, PublicationBatchResult.Error, 0, HealthStatus.Degraded)]
    [InlineData(0, PipelineStatus.Running, BatchStatus.Finished, PublicationBatchResult.ProcessedPartial, 0, HealthStatus.Degraded)]
    public async Task CheckHealth(int pipelineRetry, PipelineStatus p, BatchStatus b, PublicationBatchResult r, int seconds, HealthStatus expectedStatus)
    {
        PublicationMonitor monitor = new PubSubContext().AddPublication("any");

        monitor.Pipeline.Update(p);
        monitor.Batch.Update(b);
        monitor.Result.Update(r);
        monitor.PipelineRetryIteration = pipelineRetry;

        var check = new CheckForTest(monitor, seconds);

        HealthCheckResult result = await check.CheckHealthAsync(Context);
        result.Status.ShouldBe(expectedStatus);
    }

    private class CheckForTest : PublicationHealthCheck
    {
        private readonly int _seconds;

        public CheckForTest(PublicationMonitor monitor, int seconds) : base(new PublicationOptions(), monitor)
        {
            this._seconds = seconds;
        }

        protected override DateTime UtcNow => DateTime.UtcNow.AddSeconds(this._seconds);
    }
}