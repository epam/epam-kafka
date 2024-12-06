// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Options;
using Epam.Kafka.PubSub.Common.Pipeline;
using Epam.Kafka.PubSub.Utils;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Polly;
using Polly.Retry;

using System.Diagnostics;

namespace Epam.Kafka.PubSub.Common;

internal abstract class PubSubBackgroundService<TOptions, TBatchResult, TMonitor, TTopic> : BackgroundService
    where TMonitor : PubSubMonitor<TBatchResult>
    where TOptions : PubSubOptions
    where TBatchResult : struct, Enum
    where TTopic : IDisposable
{
    private readonly DiagnosticListener _diagnosticListener;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    protected PubSubBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        IKafkaFactory kafkaFactory,
        TOptions options,
        TMonitor monitor,
        ILoggerFactory? loggerFactory)
    {
        this._serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        this.KafkaFactory = kafkaFactory ?? throw new ArgumentNullException(nameof(kafkaFactory));
        this.Options = options ?? throw new ArgumentNullException(nameof(options));

        this.Logger = loggerFactory?.CreateLogger(monitor.FullName) ?? NullLogger.Instance;
        this.Monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        this._diagnosticListener = new DiagnosticListener(this.Monitor.FullName);
    }

    protected IKafkaFactory KafkaFactory { get; }
    protected TMonitor Monitor { get; }
    protected ILogger Logger { get; }
    protected int? AdaptiveBatchSize { get; set; }

    protected TOptions Options { get; }

    public override void Dispose()
    {
        this._diagnosticListener.Dispose();

        base.Dispose();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        if (!this.Options.Enabled)
        {
            this.Monitor.Pipeline.Update(PipelineStatus.Disabled);
            return;
        }

        try
        {
            this.WaitDependencies(stoppingToken);
        }
        catch (Exception exception)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                this.Monitor.Pipeline.Update(PipelineStatus.Cancelled);
            }
            else
            {
                this.Monitor.Pipeline.Update(PipelineStatus.Failed);

                this.Logger.PipelineFailed(exception, this.Monitor.Name);
            }

            throw;
        }

        this.ExecutePipeline(stoppingToken);
    }

    private void WaitDependencies(CancellationToken stoppingToken)
    {
        int count = this.Options.WaitForDependencies.Count;

        if (count > 0)
        {
            this.Logger.WaitBegin(this.Monitor.Name, count);

            using IServiceScope scope = this._serviceScopeFactory.CreateScope();

            Task[] tasks = this.Options.WaitForDependencies.Select(x => x.Invoke(scope.ServiceProvider)).ToArray();

            this.Monitor.Pipeline.Update(PipelineStatus.None);

            Task.WaitAll(tasks, stoppingToken);

            this.Logger.WaitEnd(this.Monitor.Name);

            this.Monitor.Pipeline.Update(PipelineStatus.None);
        }
    }

    private RetryPolicy<TBatchResult> GetBatchRetryPolicy(CancellationToken stoppingToken)
    {
        return Policy<TBatchResult>
            .Handle<Exception>(e => e.RetryBatchAllowed() && !stoppingToken.IsCancellationRequested)
            .WaitAndRetry(this.Options.BatchRetryCount,
                i => TimeSpan.FromSeconds(Math.Min(this.Options.BatchRetryMaxTimeout.TotalSeconds, Math.Pow(i, 2))),
                (result, timeout, iteration, _) =>
                {
                    Exception exception = result.Exception;

                    this.HandleBatchSizeReduction(iteration);

                    this.Monitor.Batch.Update(BatchStatus.Finished);

                    this.Monitor.Result.Update((TBatchResult)(object)BatchResult.Error);

                    this.Logger.BatchRetry(exception, this.Monitor.Name, iteration,
                        this.Options.BatchRetryCount, timeout, this.AdaptiveBatchSize ?? this.Options.BatchSize);
                });
    }

    private void ExecutePipeline(CancellationToken stoppingToken)
    {
        TTopic? topicWrapper = default;

        ISyncPolicy<TBatchResult> batchPolicy = this.GetBatchRetryPolicy(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            this.Monitor.Pipeline.Update(PipelineStatus.Running);

            try
            {
                topicWrapper ??= this.CreateTopicWrapper();

                TBatchResult batchResult = this.ExecuteBatchWrapper(batchPolicy, topicWrapper, stoppingToken);

                this.Monitor.HandleResult(batchResult);

                this.BatchFinishedTimeout(batchResult, stoppingToken);
            }
            catch (Exception exception)
            {
                this.Monitor.Batch.Update(BatchStatus.None);

                topicWrapper?.Dispose();
                topicWrapper = default;

                if (stoppingToken.IsCancellationRequested)
                {
                    this.Monitor.Pipeline.Update(PipelineStatus.Cancelled);

                    return;
                }

                this.Monitor.Result.Update((TBatchResult)(object)BatchResult.Error);

                if (this.Monitor.PipelineRetryIteration >= this.Options.PipelineRetryCount ||
                    !exception.RetryPipelineAllowed())
                {
                    this.Monitor.Pipeline.Update(PipelineStatus.Failed);

                    this.Logger.PipelineFailed(exception, this.Monitor.Name);

                    throw;
                }

                this.Monitor.PipelineRetryIteration++;

                this.Monitor.Pipeline.Update(PipelineStatus.RetryTimeout);

                this.Logger.PipelineRetry(exception, this.Monitor.Name, this.Monitor.PipelineRetryIteration,
                    this.Options.PipelineRetryCount, this.Options.PipelineRetryTimeout);

                Task.Delay(this.Options.PipelineRetryTimeout, stoppingToken).Wait(stoppingToken);
            }
        }

        topicWrapper?.Dispose();
        this.Monitor.Pipeline.Update(PipelineStatus.Cancelled);
        this.Monitor.Batch.Update(BatchStatus.None);
    }

    protected abstract TTopic CreateTopicWrapper();

    protected static T ResolveRequiredService<T>(IServiceProvider sp, Type type)
        where T : notnull
    {
        try
        {
            return (T)sp.GetRequiredService(type);
        }
        catch (Exception e)
        {
            if (e.Source == "Microsoft.Extensions.DependencyInjection")
            {
                e.DoNotRetryPipeline();
            }
            else
            {
                e.DoNotRetryBatch();
            }

            throw;
        }
    }

    private void BatchFinishedTimeout(TBatchResult subBatchResult, CancellationToken stoppingToken)
    {
        TimeSpan? timeout = null;

        if (subBatchResult.Equals((TBatchResult)(object)BatchResult.Empty) && this.Options.BatchEmptyTimeout.Ticks > 0)
        {
            timeout = this.Options.BatchEmptyTimeout;
        }

        timeout ??= this.GetBatchFinishedTimeout(subBatchResult);

        this.Monitor.Batch.Update(BatchStatus.Finished);

        if (timeout.HasValue)
        {
            Task.Delay(timeout.Value, stoppingToken).Wait(stoppingToken);
        }
    }

    protected abstract TimeSpan? GetBatchFinishedTimeout(TBatchResult subBatchResult);

    private TBatchResult ExecuteBatchWrapper(
        ISyncPolicy<TBatchResult> batchPolicy,
        TTopic topicWrapper,
        CancellationToken stoppingToken)
    {
        return batchPolicy.Execute(ct =>
        {
            using ActivityWrapper transaction = new(this._diagnosticListener, this.Monitor.FullName);

            IServiceScope serviceScope = this._serviceScopeFactory.CreateScope();

            var exceptions = new List<Exception>(2);

            try
            {
                IServiceProvider sp = serviceScope.ServiceProvider;

                this.ExecuteBatch(topicWrapper, sp, transaction, ct);

                this.AdaptiveBatchSize = null;

                transaction.SetResult(this.Monitor.Result.Value);

                return this.Monitor.Result.Value;
            }
            catch (Exception exception)
            {
                transaction.SetResult(exception);

                exceptions.Add(exception);

                throw;
            }
            finally
            {
                try
                {
                    serviceScope.Dispose();
                }
                catch (Exception e)
                {
#pragma warning disable CA2219 // Required to not lost information about neither batch exception nor exception from serviceScope.Dispose
                    if (exceptions.Count > 0)
                    {
                        exceptions.Add(e);
                        var aggregateException = new AggregateException(exceptions);

                        if (!exceptions[0].RetryBatchAllowed())
                        {
                            aggregateException.DoNotRetryBatch();
                        }

                        throw aggregateException;
                    }

                    throw;
#pragma warning restore CA2219
                }
            }
        }, stoppingToken);
    }

    private void HandleBatchSizeReduction(int i)
    {
        if (this.AdaptiveBatchSize.HasValue && this.AdaptiveBatchSize > 1 &&
            i >= this.Options.ReduceBatchSizeRetryIteration)
        {
            this.AdaptiveBatchSize /= 2;

            if (this.AdaptiveBatchSize < 1)
            {
                this.AdaptiveBatchSize = 1;
            }
        }
    }

    protected abstract void ExecuteBatch(
        TTopic topic,
        IServiceProvider sp,
        ActivityWrapper activitySpan,
        CancellationToken cancellationToken);
}