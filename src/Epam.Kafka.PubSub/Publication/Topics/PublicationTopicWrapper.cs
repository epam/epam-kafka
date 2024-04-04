// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Publication.Options;
using Epam.Kafka.PubSub.Publication.Pipeline;
using Epam.Kafka.PubSub.Utils;

using Microsoft.Extensions.Logging;

using System.Diagnostics;

namespace Epam.Kafka.PubSub.Publication.Topics;

internal class PublicationTopicWrapper<TKey, TValue> : IPublicationTopicWrapper<TKey, TValue>
{
    private bool _transactionActive;
    private bool _transactionInitialized;

    public PublicationTopicWrapper(
        IKafkaFactory kafkaFactory,
        PublicationMonitor monitor,
        ProducerConfig config,
        PublicationOptions options,
        ILogger logger,
        ISerializer<TKey>? keySerializer,
        ISerializer<TValue>? valueSerializer,
        ProducerPartitioner? partitioner)
    {
        this.Monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        this.Options = options ?? throw new ArgumentNullException(nameof(options));

        this.RequireTransaction = config.TransactionalId != null;

        if (this.RequireTransaction)
        {
            config.TransactionTimeoutMs ??= 60_000;

            this.MinRemaining = TimeSpan.FromMilliseconds(config.TransactionTimeoutMs.Value * 2);

            if (config.SocketTimeoutMs.HasValue &&
                config.TransactionTimeoutMs.Value - config.SocketTimeoutMs.Value < 100)
            {
                config.SocketTimeoutMs = config.TransactionTimeoutMs - 1000;
            }

            if (!config.TransactionalId!.EndsWith(this.Monitor.Name, StringComparison.OrdinalIgnoreCase))
            {
                config.TransactionalId += $"-{this.Monitor.Name}";
            }
        }
        else
        {
            config.MessageTimeoutMs ??= 60_000;
            this.MinRemaining = TimeSpan.FromMilliseconds(config.MessageTimeoutMs.Value * 2);
        }

        config.EnableDeliveryReports = true;
        config.DeliveryReportFields ??= "timestamp,status";

        this.Producer = kafkaFactory.CreateProducer<TKey, TValue>(config, this.Options.Cluster, b =>
        {
            partitioner?.Apply(b);

            if (keySerializer != null)
            {
                b.SetKeySerializer(keySerializer);
            }

            if (valueSerializer != null)
            {
                b.SetValueSerializer(valueSerializer);
            }
        });
    }

    private PublicationMonitor Monitor { get; }
    private ILogger Logger { get; }
    private TimeSpan MinRemaining { get; }
    private PublicationOptions Options { get; }
    private IProducer<TKey, TValue> Producer { get; }
    public bool RequireTransaction { get; }
    public DateTimeOffset? TransactionEnd { get; private set; }

    public void CommitTransactionIfNeeded(ActivityWrapper apm)
    {
        if (this.RequireTransaction && this._transactionActive)
        {
            using (apm.CreateSpan("commit_transaction"))
            {
                this.Producer.CommitTransaction();
                this._transactionActive = false;
            }
        }
    }

    public void AbortTransactionIfNeeded(ActivityWrapper apm)
    {
        if (this.RequireTransaction && this._transactionActive)
        {
            this.Logger.TransactionAbort(this.Monitor.Name);

            using (apm.CreateSpan("abort_transaction"))
            {
                this.Producer.AbortTransaction();
                this._transactionActive = false;
            }
        }
    }

    public IDictionary<TopicMessage<TKey, TValue>, DeliveryReport> Produce(
        IReadOnlyCollection<TopicMessage<TKey, TValue>> items,
        ActivityWrapper activitySpan,
        Stopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        this.BeginTransactionIfNeeded(activitySpan);

        cancellationToken.ThrowIfCancellationRequested();

        Dictionary<TopicMessage<TKey, TValue>, DeliveryReport> result = new(items.Count);

        TimeSpan remaining = this.Options.HandlerTimeout - stopwatch.Elapsed;

        if (remaining < this.MinRemaining)
        {
            throw new InvalidOperationException(
                $"Remaining producer time '{remaining}' less than minimum remaining time {this.MinRemaining} for '{this.Monitor.Name}'.");
        }

        using (activitySpan.CreateSpan("produce"))
        {
            int itemsToWait = 0;

            foreach (TopicMessage<TKey, TValue> item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    this.Producer.Produce(item.Topic ?? this.Options.DefaultTopic, item,
                        x => result.Add(item, DeliveryReport.FromGenericReport(x)));
                }
                catch (ProduceException<TKey, TValue> pe)
                {
                    DeliveryResult<TKey, TValue> x = pe.DeliveryResult;
                    result.Add(item,
                        new DeliveryReport(x.Topic, x.Partition, x.Offset, pe.Error, x.Status, x.Timestamp));

                    // stop producing without throwing error to be able to report errors
                    break;
                }
                finally
                {
                    itemsToWait++;
                }
            }

            while (result.Count < itemsToWait)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Task.Delay(100, cancellationToken).Wait(cancellationToken);
            }
        }

        return result;
    }

    public void Dispose()
    {
        this.Logger.ProducerClosing(this.Monitor.Name, this.Producer.Name);

#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            this.Producer.Dispose();
        }
        catch (Exception exception)
        {
            this.Logger.ProducerDisposeError(exception, this.Monitor.Name);
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    private void BeginTransactionIfNeeded(ActivityWrapper apm)
    {
        if (this.RequireTransaction)
        {
            if (!this._transactionInitialized)
            {
                this.Logger.TransactionInit(this.Monitor.Name);

                using (apm.CreateSpan("init_transactions"))
                {
                    this.Producer.InitTransactions(this.MinRemaining);
                    this._transactionInitialized = true;
                }
            }

            using (apm.CreateSpan("begin_transaction"))
            {
                this.Producer.BeginTransaction();
                this._transactionActive = true;
                this.TransactionEnd = DateTimeOffset.UtcNow + this.MinRemaining;
            }
        }
    }
}