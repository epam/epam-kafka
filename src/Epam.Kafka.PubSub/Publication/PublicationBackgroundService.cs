// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Confluent.SchemaRegistry;

using Epam.Kafka.PubSub.Common;
using Epam.Kafka.PubSub.Common.Pipeline;
using Epam.Kafka.PubSub.Publication.Metrics;
using Epam.Kafka.PubSub.Publication.Options;
using Epam.Kafka.PubSub.Publication.Pipeline;
using Epam.Kafka.PubSub.Publication.Topics;
using Epam.Kafka.PubSub.Utils;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Polly;

using System.Diagnostics;

namespace Epam.Kafka.PubSub.Publication;

internal class PublicationBackgroundService<TKey, TValue, THandler> : PubSubBackgroundService<PublicationOptions,
    PublicationBatchResult, PublicationMonitor, IPublicationTopicWrapper<TKey, TValue>>
    where THandler : IPublicationHandler<TKey, TValue>
{
    private readonly PublicationHealthMetrics _healthMeter;
    private readonly PublicationStatusMetrics _statusMeter;

    public PublicationBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        IKafkaFactory kafkaFactory,
        PublicationOptions options,
        PublicationMonitor monitor,
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

    protected override IPublicationTopicWrapper<TKey, TValue> CreateTopicWrapper()
    {
        var registry = new Lazy<ISchemaRegistryClient>(() =>
            this.KafkaFactory.GetOrCreateSchemaRegistryClient(this.Options.Cluster));

        ISerializer<TKey>? ks;
        ISerializer<TValue>? vs;

        try
        {
            ks = (ISerializer<TKey>?)this.Options.KeySerializer?.Invoke(registry);
            vs = (ISerializer<TValue>?)this.Options.ValueSerializer?.Invoke(registry);
        }
        catch (Exception exception)
        {
            exception.DoNotRetryPipeline();
            throw;
        }

        ProducerConfig config = this.KafkaFactory.CreateProducerConfig(this.Options.Producer);

        bool implicitPreprocessor = ks != null || vs != null || config.TransactionalId != null;

        if (this.Options.SerializationPreprocessor ?? implicitPreprocessor)
        {
            return new PublicationSerializeKeyAndValueTopicWrapper<TKey, TValue>(this.KafkaFactory, this.Monitor,
                config, this.Options, this.Logger,
                ks, vs, this.Options.Partitioner);
        }

        return new PublicationTopicWrapper<TKey, TValue>(this.KafkaFactory, this.Monitor, config, this.Options,
            this.Logger,
            ks, vs, this.Options.Partitioner);
    }

    protected override TimeSpan? GetBatchFinishedTimeout(PublicationBatchResult subBatchResult)
    {
        return null;
    }

    private void ExecuteBatchInternal(
        THandler state,
        IPublicationTopicWrapper<TKey, TValue> topicWrapper,
        ActivityWrapper activitySpan,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        IReadOnlyCollection<TopicMessage<TKey, TValue>> items = this.ReadItems(state, topicWrapper, activitySpan,
            cancellationToken);

        if (items.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            this.Logger.PubBatchBegin(this.Monitor.Name, items.Count);

            this.Monitor.Batch.Update(BatchStatus.Running);

            IDictionary<TopicMessage<TKey, TValue>, DeliveryReport> reports =
                topicWrapper.Produce(items, activitySpan, stopwatch, cancellationToken);

            this.Monitor.Batch.Update(BatchStatus.Commiting);

            using (activitySpan.CreateSpan("src_report"))
            {
                state.ReportResults(reports, topicWrapper.TransactionEnd, cancellationToken);
            }

            bool allItemsProcessed = this.VerifyResult(reports, items);

            if (topicWrapper.RequireTransaction)
            {
                if (allItemsProcessed)
                {
                    topicWrapper.CommitTransactionIfNeeded(activitySpan);

                    using (activitySpan.CreateSpan("src_commit"))
                    {
                        state.TransactionCommitted(cancellationToken);
                    }
                }
                else
                {
                    topicWrapper.AbortTransactionIfNeeded(activitySpan);
                }
            }

            this.Logger.BatchHandlerExecuted(items.Count,
                reports.Select(x => x.Value).GroupBy(x => $"{x.TopicPartition} {x.Status} {x.Error}")
                    .Select(g => new KeyValuePair<string, int>(g.Key, g.Count())),
                allItemsProcessed ? LogLevel.Information : LogLevel.Warning);

            this.Monitor.Result.Update(
                allItemsProcessed ? PublicationBatchResult.Processed : PublicationBatchResult.ProcessedPartial);
        }
        else
        {
            this.Logger.BatchEmpty(this.Monitor.Name);

            this.Monitor.Result.Update(PublicationBatchResult.Empty);
        }
    }

    private bool VerifyResult(IDictionary<TopicMessage<TKey, TValue>, DeliveryReport> reports,
        IReadOnlyCollection<TopicMessage<TKey, TValue>> items)
    {
        int processed = reports.Count(x => !x.Value.Error.IsError && x.Value.Status == PersistenceStatus.Persisted);
        int errors = reports.Count(x => x.Value.Error.IsError);
        int unknown = items.Count - reports.Count;

        string statusString = $"Processed: {processed}, Errors: {errors}, Skipped: {unknown}";

        DeliveryReport? firstError = reports.Values.FirstOrDefault(x => x.Error.IsError);
        KafkaException? kafkaException = firstError == null ? null : new KafkaException(firstError.Error);

        if (errors == items.Count)
        {
            throw new InvalidOperationException(
                $"All items failed to publish for {this.Monitor.Name}. {statusString}",
                kafkaException);
        }

        return processed == items.Count;
    }

    private IReadOnlyCollection<TopicMessage<TKey, TValue>> ReadItems(THandler state,
        IPublicationTopicWrapper<TKey, TValue> topicWrapper, ActivityWrapper activitySpan,
        CancellationToken cancellationToken)
    {
        this.Monitor.Batch.Update(BatchStatus.Reading);

        int countToRead = this.AdaptiveBatchSize ?? this.Options.BatchSize;

        using ActivityWrapper wrapper = activitySpan.CreateSpan("src_read");

        IReadOnlyCollection<TopicMessage<TKey, TValue>> items = state.GetBatch(countToRead,
            topicWrapper.RequireTransaction, cancellationToken);

        wrapper.SetResult(items.Count);

        this.AdaptiveBatchSize ??= this.Options.BatchSize;

        return items;
    }

    protected override void ExecuteBatch(
        IPublicationTopicWrapper<TKey, TValue> topic,
        IServiceProvider sp,
        ActivityWrapper activitySpan,
        CancellationToken cancellationToken)
    {
        THandler state = ResolveRequiredService<THandler>(sp);

        ISyncPolicy handlerPolicy = sp.GetRequiredService<PubSubContext>().GetHandlerPolicy(this.Options);

        if (this.Options.HandlerConcurrencyGroup.HasValue)
        {
            this.Monitor.Batch.Update(BatchStatus.Queued);
        }

        try
        {
            handlerPolicy.Execute(ct => this.ExecuteBatchInternal(state, topic, activitySpan, ct),
                cancellationToken);
        }
        catch
        {
            topic.AbortTransactionIfNeeded(activitySpan);

            throw;
        }
    }
}