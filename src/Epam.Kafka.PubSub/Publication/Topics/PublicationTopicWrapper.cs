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

            if (!monitor.TryRegisterTransactionId(config, out string? existing))
            {
                var exception = new InvalidOperationException(
                    $"Unable to use '{config.TransactionalId}' transactional.id in '{monitor.Name}' publication because it already used by '{existing}'.");
                exception.DoNotRetryBatch();

                throw exception;
            }
        }
        else
        {
            config.MessageTimeoutMs ??= 60_000;
            this.MinRemaining = TimeSpan.FromMilliseconds(config.MessageTimeoutMs.Value * 2);
        }

        ConfigureReports(config);

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

        // warmup to avoid potential issues with OAuth handler
        this.Producer.Poll(TimeSpan.Zero);
    }

    private static void ConfigureReports(ProducerConfig config)
    {
        const string timestamp = "timestamp";
        const string status = "status";

        config.EnableDeliveryReports = true;

        if (string.IsNullOrWhiteSpace(config.DeliveryReportFields))
        {
            config.DeliveryReportFields = $"{timestamp},{status}";
        }
        else if (!string.Equals("all", config.DeliveryReportFields, StringComparison.OrdinalIgnoreCase))
        {
            var items = config.DeliveryReportFields.Split(',').ToList();

            if (!items.Any(x => string.Equals(timestamp, x, StringComparison.OrdinalIgnoreCase)))
            {
                items.Add(timestamp);
            }

            if (!items.Any(x => string.Equals(status, x, StringComparison.OrdinalIgnoreCase)))
            {
                items.Add(status);
            }

            config.DeliveryReportFields = string.Join(",", items);
        }

        config.DeliveryReportFields = config.DeliveryReportFields.ToLowerInvariant();
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
                try
                {
                    this.Producer.CommitTransaction();
                }
                catch (Exception e)
                {
                    e.DoNotRetryBatch();
                    throw;
                }
                finally
                {
                    this._transactionActive = false;
                }
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
                try
                {
                    this.Producer.AbortTransaction();
                }
                catch (Exception e)
                {
                    e.DoNotRetryBatch();
                    throw;
                }
                finally
                {
                    this._transactionActive = false;
                }
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