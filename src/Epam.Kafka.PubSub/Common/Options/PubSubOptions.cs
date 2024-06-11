// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Pipeline;
using Epam.Kafka.PubSub.Subscription;
using Epam.Kafka.PubSub.Subscription.Pipeline;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using Polly.Timeout;

namespace Epam.Kafka.PubSub.Common.Options;

/// <summary>
///     Options to configure subscription or publication processor behaviour.
/// </summary>
public abstract class PubSubOptions
{
    internal readonly List<Func<IServiceProvider, Task>> WaitForDependencies = new();

    internal Type? KeyType;
    internal Type? ValueType;

    /// <summary>
    ///     Whether processing enabled. Default: <c>true</c>
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     The max number of items to be processed in one batch. Default: <c>100</c>
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    ///     Logical name for cluster that will be used to create consumer or producer via <see cref="IKafkaFactory" />.
    /// </summary>
    public string? Cluster { get; set; }

    /// <summary>
    ///     Number of pipeline processing retries. Default: <c>int.MaxValue</c>.
    ///     <remarks>
    ///         When number consecutive pipeline failures exceeds this value exception will be thrown to terminate
    ///         background service.
    ///     </remarks>
    /// </summary>
    public int PipelineRetryCount { get; set; } = int.MaxValue;

    /// <summary>
    ///     Timeout between pipeline processing retry attempts. Default: <c>TimeSpan.FromMinutes(5)</c>
    /// </summary>
    public TimeSpan PipelineRetryTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     Number of batch processing retries. Default: <c>10</c>.
    ///     <remarks>
    ///         When number consecutive batch failures exceeds this value exception will be thrown to produce pipeline
    ///         processing failure.
    ///     </remarks>
    /// </summary>
    public int BatchRetryCount { get; set; } = 10;

    /// <summary>
    ///     Max timeout for batch processing retry. Default <c>TimeSpan.FromMinutes(5)</c>.
    ///     <remarks>
    ///         Batch timeout depends on retry iteration (i) and calculated using
    ///         <c>TimeSpan.FromSeconds(Math.Min(this.Options.BatchRetryMaxTimeout.TotalSeconds, Math.Pow(i, 2)))</c> formula.
    ///     </remarks>
    /// </summary>
    public TimeSpan BatchRetryMaxTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     Timeout value used if no message was available for previous batch. Default <c>TimeSpan.Zero</c> for subscription
    ///     and <c>TimeSpan.FromSeconds(5)</c> for publication.
    /// </summary>
    public TimeSpan BatchEmptyTimeout { get; set; } = TimeSpan.Zero;

    /// <summary>
    ///     Batch retry iteration after which batch size may be temporary reduced. Default <c>2</c>.
    ///     <remarks>
    ///         If number of batch retry iterations greater or equal to this value, then batch size will be divided by 2 on
    ///         each retry.
    ///         Batch size will be restored to original value in case of successful batch processing or new pipeline iteration.
    ///     </remarks>
    /// </summary>
    public int ReduceBatchSizeRetryIteration { get; set; } = 2;

    /// <summary>
    ///     Timeout for subscription or publication handler execution. Default <c>TimeSpan.FromMinutes(5)</c>.
    /// </summary>
    public TimeSpan HandlerTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     The <see cref="TimeoutStrategy" /> that will be applied for processing handler.
    /// </summary>
    public TimeoutStrategy HandlerTimeoutStrategy { get; set; } = TimeoutStrategy.Optimistic;

    /// <summary>
    ///     The handler concurrency group value.
    ///     <remarks>
    ///         By default subscriptions and publications executed in parallel.
    ///         Optionally they can be associated with 'concurrency group'.
    ///         Handlers within same 'concurrency group' value executed sequentially.
    ///         For subscriptions consume from kafka topics still will run in parallel,
    ///         only invocation of <see cref="ISubscriptionHandler{TKey,TValue}" /> will be synchronized.
    ///     </remarks>
    /// </summary>
    public int? HandlerConcurrencyGroup { get; set; }

    /// <summary>
    ///     Used for health checks. Default: <c>TimeSpan.FromMinutes(3)</c>.
    ///     <remarks>
    ///         Max time that pipeline can be in state <see cref="PipelineStatus.None" /> before considering
    ///         <see cref="HealthStatus.Unhealthy" />. Default <c>TimeSpan.FromMinutes(3)</c>.
    ///     </remarks>
    /// </summary>
    public TimeSpan HealthChecksThresholdPipeline { get; set; } = TimeSpan.FromMinutes(3);

    /// <summary>
    ///     Used for health checks. Default: <c>TimeSpan.FromMinutes(1)</c>.
    ///     <remarks>
    ///         For subscription:
    ///         <list type="string">
    ///             Max time that batch can be in states
    ///             <see cref="BatchStatus.None" />, <see cref="BatchStatus.Reading" />, <see cref="BatchStatus.Finished" />,
    ///             <see cref="BatchStatus.Commiting" />
    ///             before considering <see cref="HealthStatus.Unhealthy" />.
    ///         </list>
    ///         <list type="string">
    ///             Max time that batch last result can be <see cref="SubscriptionBatchResult.NotAssigned" />
    ///             before considering <see cref="HealthStatus.Degraded" />.
    ///         </list>
    ///         <list type="string">
    ///             Max time that batch can be in state  <see cref="BatchStatus.Queued" /> without associated
    ///             <see cref="HandlerConcurrencyGroup" /> before considering <see cref="HealthStatus.Unhealthy" />.
    ///         </list>
    ///         For publication:
    ///         <list type="string">
    ///             Max time that batch can be in states
    ///             <see cref="BatchStatus.None" />, <see cref="BatchStatus.Finished" />
    ///             before considering <see cref="HealthStatus.Unhealthy" />.
    ///         </list>
    ///         <list type="string">
    ///             Max time that batch can be in state  <see cref="BatchStatus.Queued" /> without associated
    ///             <see cref="HandlerConcurrencyGroup" /> before considering <see cref="HealthStatus.Unhealthy" />.
    ///         </list>
    ///     </remarks>
    /// </summary>
    public TimeSpan HealthChecksThresholdBatch { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     Max time that batch can be in <see cref="BatchStatus.Queued" /> state before considering degraded. If <c>null</c>
    ///     (default) then value of <see cref="HandlerTimeout" /> will be used.
    /// </summary>
    public TimeSpan? HealthChecksMaxQueued { get; set; }
}