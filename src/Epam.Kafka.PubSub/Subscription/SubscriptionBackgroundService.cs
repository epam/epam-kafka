// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Confluent.SchemaRegistry;

using Epam.Kafka.PubSub.Common;
using Epam.Kafka.PubSub.Common.Pipeline;
using Epam.Kafka.PubSub.Subscription.Metrics;
using Epam.Kafka.PubSub.Subscription.Options;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.PubSub.Subscription.State;
using Epam.Kafka.PubSub.Subscription.Topics;
using Epam.Kafka.PubSub.Utils;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;

namespace Epam.Kafka.PubSub.Subscription;

internal class SubscriptionBackgroundService<TKey, TValue> : PubSubBackgroundService<
    SubscriptionOptions, SubscriptionBatchResult, SubscriptionMonitor, SubscriptionTopicWrapper<TKey, TValue>>
{
    private readonly IOptionsMonitor<SubscriptionOptions> _optionsMonitor;
    private readonly SubscriptionHealthMetrics _healthMeter;
    private readonly SubscriptionStatusMetrics _statusMeter;

    public SubscriptionBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        IKafkaFactory kafkaFactory,
        IOptionsMonitor<SubscriptionOptions> optionsMonitor,
        SubscriptionMonitor monitor,
        ILoggerFactory? loggerFactory) : base(
        serviceScopeFactory,
        kafkaFactory,
        optionsMonitor.Get(monitor.Name),
        monitor,
        loggerFactory)
    {
        this._optionsMonitor = optionsMonitor;
        Type handlerType = this.Options.HandlerType ?? throw new ArgumentException("HandlerType is null", nameof(optionsMonitor));

        if (!typeof(ISubscriptionHandler<TKey, TValue>).IsAssignableFrom(handlerType))
        {
            throw new ArgumentException($"HandlerType {handlerType} not assignable to {typeof(ISubscriptionHandler<TKey, TValue>)}", nameof(optionsMonitor));
        }

        this._statusMeter = new(this.Monitor);
        this._healthMeter = new(this.Options, this.Monitor);
    }

    public override void Dispose()
    {
        base.Dispose();

        this._statusMeter.Dispose();
        this._healthMeter.Dispose();
    }

    protected override SubscriptionTopicWrapper<TKey, TValue> CreateTopicWrapper()
    {
        var registry = new Lazy<ISchemaRegistryClient>(() =>
            this.KafkaFactory.GetOrCreateSchemaRegistryClient(this.Options.Cluster));

        IDeserializer<TKey>? ks;
        IDeserializer<TValue>? vs;
        try
        {
            ks = (IDeserializer<TKey>?)this.Options.KeyDeserializer?.Invoke(registry);
            vs = (IDeserializer<TValue>?)this.Options.ValueDeserializer?.Invoke(registry);
        }
        catch (Exception exception)
        {
            exception.DoNotRetryPipeline();
            throw;
        }

        return new SubscriptionTopicWrapper<TKey, TValue>(this.KafkaFactory, this.Monitor, this._optionsMonitor, this.Options, 
            ks, vs, this.Logger);
    }

    protected override TimeSpan? GetBatchFinishedTimeout(SubscriptionBatchResult subscriptionBatchResult)
    {
        TimeSpan? timeout = null;

        if (subscriptionBatchResult == SubscriptionBatchResult.Paused && this.Options.BatchPausedTimeout.Ticks > 0)
        {
            timeout = this.Options.BatchPausedTimeout;
        }

        if (subscriptionBatchResult == SubscriptionBatchResult.NotAssigned &&
            this.Options.BatchNotAssignedTimeout.Ticks > 0)
        {
            timeout = this.Options.BatchNotAssignedTimeout;
        }

        return timeout;
    }

    protected override void ExecuteBatch(
        SubscriptionTopicWrapper<TKey, TValue> topic,
        IServiceProvider sp,
        ActivityWrapper activitySpan,
        CancellationToken cancellationToken)
    {
        BatchState state = sp.ResolveRequiredService<BatchState>(this.Options.StateType);

        this.Monitor.Batch.Update(BatchStatus.Reading);

        bool unassignedBeforeRead = state.GetBatch(
            topic, activitySpan, out IReadOnlyCollection<ConsumeResult<TKey, TValue>> batch, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        if (this.AdaptiveBatchSize.HasValue)
        {
            batch = batch.Take(this.AdaptiveBatchSize.Value).ToList();
        }
        else
        {
            this.AdaptiveBatchSize = batch.Count;
        }

        if (batch.Count > 0)
        {
            batch.GetOffsetsRange(
                out IDictionary<TopicPartition, Offset>? from,
                out IDictionary<TopicPartition, Offset>? to);

            this.Logger.SubBatchBegin(this.Monitor.Name, batch.Count,
                from.Select(x => new TopicPartitionOffset(x.Key, x.Value)),
                to.Select(x => new TopicPartitionOffset(x.Key, x.Value)));

            this.CreateAndExecuteHandler(sp, topic, batch, activitySpan, cancellationToken);

            this.Monitor.Batch.Update(BatchStatus.Commiting);

            state.CommitResults(topic, activitySpan,
                // offset of processed message + 1
                to.PrepareOffsetsToCommit(), cancellationToken);

            this.Monitor.Result.Update(SubscriptionBatchResult.Processed);
        }
        else
        {
            // get new instance of assignment list
            List<TopicPartition> assignments = topic.Consumer.Assignment;

            if (assignments.Count > 0 &&
                assignments.All(x => topic.TryGetOffset(x, out Offset offset) && offset == ExternalOffset.Paused))
            {
                this.Monitor.Result.Update(SubscriptionBatchResult.Paused);

                this.Logger.ConsumerPaused(this.Monitor.Name, assignments);
            }
            else if (assignments.Count == 0 || unassignedBeforeRead)
            {
                this.Monitor.Result.Update(SubscriptionBatchResult.NotAssigned);

                this.Logger.ConsumerNotAssigned(this.Monitor.Name);
            }
            else
            {
                this.Logger.BatchEmpty(this.Monitor.Name);

                this.Monitor.Result.Update(SubscriptionBatchResult.Empty);
            }
        }
    }

    private void CreateAndExecuteHandler(IServiceProvider sp,
        SubscriptionTopicWrapper<TKey, TValue> topic,
        IReadOnlyCollection<ConsumeResult<TKey, TValue>> batch,
        ActivityWrapper activitySpan,
        CancellationToken cancellationToken)
    {
        ISubscriptionHandler<TKey, TValue> handler = this.CreateHandler(sp, activitySpan, topic);

        ISyncPolicy handlerPolicy = this.Monitor.Context.GetHandlerPolicy(this.Options);

        if (this.Options.HandlerConcurrencyGroup.HasValue)
        {
            this.Monitor.Batch.Update(BatchStatus.Queued);
        }

        handlerPolicy.Execute(ct =>
        {
            this.Monitor.Batch.Update(BatchStatus.Running);

            using (activitySpan.CreateSpan("process"))
            {
                handler.Execute(batch, ct);
            }
        }, cancellationToken);
    }

    protected virtual ISubscriptionHandler<TKey, TValue> CreateHandler(IServiceProvider sp, ActivityWrapper activitySpan, SubscriptionTopicWrapper<TKey, TValue> topic)
    {
        ISubscriptionHandler<TKey, TValue> handler =
            sp.ResolveRequiredService<ISubscriptionHandler<TKey, TValue>>(this.Options.HandlerType!);

        return handler;
    }
}