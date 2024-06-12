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

using Polly;

namespace Epam.Kafka.PubSub.Subscription;

internal sealed class SubscriptionBackgroundService<TKey, TValue, THandler> : PubSubBackgroundService<
    SubscriptionOptions, SubscriptionBatchResult, SubscriptionMonitor, SubscriptionTopicWrapper<TKey, TValue>>
    where THandler : ISubscriptionHandler<TKey, TValue>
{
    private readonly SubscriptionHealthMetrics _healthMeter;
    private readonly SubscriptionStatusMetrics _statusMeter;

    public SubscriptionBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        IKafkaFactory kafkaFactory,
        SubscriptionOptions options,
        SubscriptionMonitor monitor,
        ILoggerFactory? loggerFactory) : base(
        serviceScopeFactory,
        kafkaFactory,
        options,
        monitor,
        loggerFactory)
    {
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

        return new SubscriptionTopicWrapper<TKey, TValue>(this.KafkaFactory, this.Monitor, this.Options, ks, vs,
            this.Logger);
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
        BatchState state = ResolveRequiredService<BatchState>(sp, this.Options.StateType);

        bool unassignedBeforeRead = topic.Consumer.Assignment.Count == 0;

        this.Monitor.Batch.Update(BatchStatus.Reading);

        IReadOnlyCollection<ConsumeResult<TKey, TValue>> batch = state.GetBatch(
            topic, activitySpan, cancellationToken);

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
            PrepareOffsetsToCommit(batch, out IDictionary<TopicPartition, Offset>? from,
                out IDictionary<TopicPartition, Offset>? to);

            this.Logger.SubBatchBegin(this.Monitor.Name, batch.Count,
                from.Select(x => new TopicPartitionOffset(x.Key, x.Value)),
                to.Select(x => new TopicPartitionOffset(x.Key, x.Value)));

            this.CreateAndExecuteHandler(sp, batch, activitySpan, cancellationToken);

            this.Monitor.Batch.Update(BatchStatus.Commiting);

            // offset of processed message + 1
            state.CommitResults(topic, activitySpan,
                to.Select(x => new TopicPartitionOffset(x.Key, x.Value + 1)).ToList(), cancellationToken);

            this.Monitor.Result.Update(SubscriptionBatchResult.Processed);
        }
        else
        {
            // get new instance of assignment list
            List<TopicPartition> assignments = topic.Consumer.Assignment;

            if (assignments.Count > 0 &&
                assignments.All(x => topic.Offsets.TryGetValue(x, out Offset offset) && offset == Offset.End))
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

    private void CreateAndExecuteHandler(
        IServiceProvider sp,
        IReadOnlyCollection<ConsumeResult<TKey, TValue>> batch,
        ActivityWrapper activitySpan,
        CancellationToken cancellationToken)
    {
        THandler handler = ResolveRequiredService<THandler>(sp);

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

    private static void PrepareOffsetsToCommit(IEnumerable<ConsumeResult<TKey, TValue>> results,
        out IDictionary<TopicPartition, Offset> from, out IDictionary<TopicPartition, Offset> to)
    {
        from = new Dictionary<TopicPartition, Offset>();
        to = new Dictionary<TopicPartition, Offset>();

        foreach (ConsumeResult<TKey, TValue> item in results)
        {
            if (!to.TryGetValue(item.TopicPartition, out Offset currentTo))
            {
                to.Add(item.TopicPartition, item.Offset);
            }

            if (!from.TryGetValue(item.TopicPartition, out Offset currentFrom))
            {
                from.Add(item.TopicPartition, item.Offset);
            }

            if (item.Offset.Value > currentTo)
            {
                to[item.TopicPartition] = item.Offset;
            }

            if (item.Offset.Value < currentFrom)
            {
                from[item.TopicPartition] = item.Offset;
            }
        }
    }
}