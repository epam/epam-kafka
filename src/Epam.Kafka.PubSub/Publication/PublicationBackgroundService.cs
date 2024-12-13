// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

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

internal class PublicationBackgroundService<TKey, TValue> : PubSubBackgroundService<PublicationOptions,
    PublicationBatchResult, PublicationMonitor, IPublicationTopicWrapper<TKey, TValue>>
{
    private readonly Type _handlerType;
    private readonly PublicationHealthMetrics _healthMeter;
    private readonly PublicationStatusMetrics _statusMeter;

    public PublicationBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        IKafkaFactory kafkaFactory,
        PublicationOptions options,
        PublicationMonitor monitor,
        Type handlerType,
        ILoggerFactory? loggerFactory) : base(
        serviceScopeFactory,
        kafkaFactory,
        options,
        monitor,
        loggerFactory)
    {
        this._handlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));

        if (!typeof(IPublicationHandler<TKey, TValue>).IsAssignableFrom(handlerType))
        {
            throw new ArgumentException($"Type {typeof(IPublicationHandler<TKey, TValue>)} not assignable from {handlerType}", nameof(handlerType));
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

    protected override IPublicationTopicWrapper<TKey, TValue> CreateTopicWrapper()
    {
        return this.KafkaFactory.CreatePublicationTopicWrapper<TKey, TValue>(this.Options, this.Monitor, this.Logger);
    }

    protected override TimeSpan? GetBatchFinishedTimeout(PublicationBatchResult subBatchResult)
    {
        return null;
    }

    private void ExecuteBatchInternal(
        IPublicationHandler<TKey, TValue> state,
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
                topicWrapper.Produce(items, activitySpan, stopwatch, this.Options.HandlerTimeout, cancellationToken);

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

            this.Monitor.Result.Update(
                allItemsProcessed ? PublicationBatchResult.Processed : PublicationBatchResult.ProcessedPartial);

            this.Logger.BatchHandlerExecuted(items.Count,
                reports.Select(x => x.Value).GroupBy(x => $"{x.TopicPartition} {x.Status} {x.Error}")
                    .Select(g => new KeyValuePair<string, int>(g.Key, g.Count())),
                allItemsProcessed ? LogLevel.Information : LogLevel.Warning);
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

    private IReadOnlyCollection<TopicMessage<TKey, TValue>> ReadItems(IPublicationHandler<TKey, TValue> state,
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
        IPublicationHandler<TKey, TValue> handler =
            sp.ResolveRequiredService<IPublicationHandler<TKey, TValue>>(this._handlerType);

        ISyncPolicy handlerPolicy = this.Monitor.Context.GetHandlerPolicy(this.Options);

        if (this.Options.HandlerConcurrencyGroup.HasValue)
        {
            this.Monitor.Batch.Update(BatchStatus.Queued);
        }

        try
        {
            handlerPolicy.Execute(ct => this.ExecuteBatchInternal(handler, topic, activitySpan, ct),
                cancellationToken);
        }
        catch (Exception e1)
        {
            try
            {
                topic.AbortTransactionIfNeeded(activitySpan);
            }
            catch (Exception e2)
            {
                var exception = new AggregateException(e1, e2);
                exception.DoNotRetryBatch();

                throw exception;
            }

            throw;
        }
    }
}