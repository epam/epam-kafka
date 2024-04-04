// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Subscription.Options;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.PubSub.Utils;

using Microsoft.Extensions.Logging;

namespace Epam.Kafka.PubSub.Subscription.Topics;

internal sealed class SubscriptionTopicWrapper<TKey, TValue> : IDisposable
{
    private const int ConsumeTimeoutMs = 2000;

    private readonly List<ConsumeResult<TKey, TValue>> _buffer;

    private Exception? _exception;

    public SubscriptionTopicWrapper(IKafkaFactory kafkaFactory,
        SubscriptionMonitor monitor,
        SubscriptionOptions options,
        IDeserializer<TKey>? keyDeserializer,
        IDeserializer<TValue>? valueDeserializer,
        ILogger logger)
    {
        if (kafkaFactory == null)
        {
            throw new ArgumentNullException(nameof(kafkaFactory));
        }

        this.Monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        this.Options = options ?? throw new ArgumentNullException(nameof(options));
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this._buffer = new List<ConsumeResult<TKey, TValue>>(options.BatchSize);

        ConsumerConfig config = kafkaFactory.CreateConsumerConfig(options.Consumer);

        this.ConfigureConsumerConfig(config);

        this.Consumer = kafkaFactory.CreateConsumer<TKey, TValue>(config, options.Cluster, b =>
        {
            if (keyDeserializer != null)
            {
                b.SetKeyDeserializer(keyDeserializer);
            }

            if (valueDeserializer != null)
            {
                b.SetValueDeserializer(valueDeserializer);
            }

            this.ConfigureConsumerBuilder(b);
        });
    }

    public SubscriptionMonitor Monitor { get; }
    public SubscriptionOptions Options { get; }
    public ILogger Logger { get; }

    public IDictionary<TopicPartition, Offset> Offsets { get; } = new Dictionary<TopicPartition, Offset>();

    public IConsumer<TKey, TValue> Consumer { get; }
    public string? ConsumerGroup { get; private set; }

    public Func<IReadOnlyCollection<TopicPartition>, IEnumerable<TopicPartitionOffset>>? ExternalState { get; set; }

    public void Dispose()
    {
        this.Logger.ConsumerClosing(this.Monitor.Name, this.Consumer.MemberId);

#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
            this.Consumer.Close();

            this.Consumer.Dispose();
        }
        catch (Exception exception)
        {
            this.Logger.ConsumerDisposeError(exception, this.Monitor.Name);
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }

    public void ClearIfNotAssigned()
    {
        if (this.Consumer.Assignment.Count == 0 && this.Consumer.Subscription.Count == 0)
        {
            this._buffer.Clear();
            this.Offsets.Clear();
        }
    }

    public IReadOnlyCollection<ConsumeResult<TKey, TValue>> GetBatch(ActivityWrapper activitySpan,
        CancellationToken cancellationToken)
    {
        if (this._buffer.Count == 0)
        {
            using ActivityWrapper wrapper = activitySpan.CreateSpan("read");

            this.ReadToBuffer(cancellationToken);

            wrapper.SetResult(this._buffer.Count);
        }

        return this._buffer;
    }

    public void OnCommit(IReadOnlyCollection<TopicPartitionOffset> committed)
    {
        if (committed.Count > 0)
        {
            this.Logger.OffsetsCommitted(this.Monitor.Name, committed);

            // clear from buffer items that were successfully committed
            foreach (TopicPartitionOffset item in committed)
            {
                this.CleanupBuffer(x => x.TopicPartition == item.TopicPartition && x.Offset.Value < item.Offset.Value,
                    "OffsetsCommitted");

                this.Offsets[item.TopicPartition] = item.Offset;
            }
        }
    }

    public void OnReset(IReadOnlyCollection<TopicPartitionOffset> reset)
    {
        if (reset.Count > 0)
        {
            this.Logger.OffsetsReset(this.Monitor.Name, reset);

            this.CleanupBuffer(x => reset.Any(v => v.TopicPartition == x.TopicPartition), "partition offset reset");
        }
    }

    private void ConfigureConsumerBuilder(ConsumerBuilder<TKey, TValue> builder)
    {
        builder.SetPartitionsAssignedHandler((consumer, list) =>
        {
            var result = new List<TopicPartitionOffset>(list.Count);
            result.AddRange(list.Select(x => new TopicPartitionOffset(x, Offset.Unset)));

            this.OnPartitionsAssigned(consumer, result);

            return result;
        });

        builder.SetPartitionsRevokedHandler(this.OnPartitionsRevoked);
        builder.SetPartitionsLostHandler(this.OnPartitionsLost);

        builder.SetLogHandler((_, msg) => { this.Logger.KafkaLogHandler(msg); });
    }

    private void OnPartitionsLost(IConsumer<TKey, TValue> c, List<TopicPartitionOffset> list)
    {
        if (list.Count > 0)
        {
            this.Logger.PartitionsLost(this.Monitor.Name, c.MemberId, list);

            this.CleanupBuffer(x => list.Any(v => v.TopicPartition == x.TopicPartition), "partition lost");

            if (this.ExternalState != null)
            {
                foreach (TopicPartitionOffset partitionOffset in list)
                {
                    this.Offsets.Remove(partitionOffset.TopicPartition);
                }
            }
        }
    }

    private void OnPartitionsRevoked(IConsumer<TKey, TValue> c, List<TopicPartitionOffset> list)
    {
        if (list.Count > 0)
        {
            this.Logger.PartitionsRevoked(this.Monitor.Name, c.MemberId, list);

            this.CleanupBuffer(x => list.Any(v => v.TopicPartition == x.TopicPartition), "partition revoked");

            if (this.ExternalState != null)
            {
                foreach (TopicPartitionOffset partitionOffset in list)
                {
                    this.Offsets.Remove(partitionOffset.TopicPartition);
                }
            }
        }
    }

    private void OnPartitionsAssigned(IConsumer<TKey, TValue> c, List<TopicPartitionOffset> list)
    {
        if (list.Count > 0)
        {
            if (this.ExternalState != null)
            {
                var tp = list.Select(x => x.TopicPartition).ToList();

                list.Clear();

#pragma warning disable CA1031 // can't throw exceptions in handler callback because it triggers incorrect state in librdkafka and some times leads to app crash. 
                try
                {
                    TopicPartitionOffset[] state = this.ExternalState.Invoke(tp).ToArray();

                    list.AddRange(state);
                }
                catch (Exception exception)
                {
                    // Save it and throw later to trigger pipeline retry.
                    exception.DoNotRetryBatch();
                    this._exception = exception;

                    this.Logger.PartitionsAssignError(exception, this.Monitor.Name, c.MemberId, tp);

                    // set Offset.End special value to prevent reading from topic partitions until pipeline restart.
                    list.AddRange(tp.Select(x => new TopicPartitionOffset(x, Offset.End)));

                    return;
                }
#pragma warning restore CA1031
            }

            this.Logger.PartitionsAssigned(this.Monitor.Name, c.MemberId, list);
        }
    }

    private void CleanupBuffer(Predicate<ConsumeResult<TKey, TValue>> predicate, string reason)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        int count = this._buffer.RemoveAll(predicate);

        if (count > 0)
        {
            this.Logger.BufferCleanup(this.Monitor.Name, count, reason);
        }
    }

    private void ConfigureConsumerConfig(ConsumerConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        this.ConsumerGroup = config.GroupId;

        config.ClientId = $"{AppDomain.CurrentDomain.FriendlyName}@{Environment.MachineName}:{this.Monitor.Name}";

        config.EnableAutoCommit = false;
        config.EnableAutoOffsetStore = false;
        config.AutoOffsetReset ??= AutoOffsetReset.Earliest;
        config.IsolationLevel ??= IsolationLevel.ReadCommitted;

        config.PartitionAssignmentStrategy ??= PartitionAssignmentStrategy.CooperativeSticky;

        // to avoid leaving group in case of long-running processing
        config.MaxPollIntervalMs ??= (int)TimeSpan.FromMinutes(60).TotalMilliseconds;
    }

    public void CommitOffsets(ActivityWrapper activitySpan, IReadOnlyCollection<TopicPartitionOffset> offsets)
    {
        using (activitySpan.CreateSpan("commit_kafka"))
        {
            // commit to kafka also
            this.Consumer.Commit(offsets);
        }
    }

    private void ReadToBuffer(CancellationToken cancellationToken)
    {
        int batchSize = this.Options.BatchSize;

        try
        {
            while (!cancellationToken.IsCancellationRequested && this._buffer.Count < batchSize)
            {
                ConsumeResult<TKey, TValue>? consumeResult = this.Consumer.Consume(ConsumeTimeoutMs);

                if (consumeResult == null || consumeResult.IsPartitionEOF)
                {
                    break;
                }

                this._buffer.Add(consumeResult);
            }
        }
        catch (ConsumeException consumeException)
        {
            ConsumeResult<byte[], byte[]> record = consumeException.ConsumerRecord;

            switch (consumeException.Error.Code)
            {
                case ErrorCode.Local_Fatal:
                    {
                        consumeException.DoNotRetryPipeline();
                        throw;
                    }
                case ErrorCode.Local_KeyDeserialization:
                case ErrorCode.Local_ValueDeserialization:
                    {
                        // start next batch from message skipped for current batch.
                        this.Consumer.Seek(record.TopicPartitionOffset);
                        // will be thrown later if buffer is empty
                        break;
                    }

                default:
                    {
                        consumeException.DoNotRetryBatch();
                        throw;
                    }
            }

            // unable to return at least something, so only throw
            if (this._buffer.Count == 0)
            {
                throw;
            }

            this.Logger.ConsumeError(consumeException, this.Monitor.Name,
                consumeException.Error, record.TopicPartitionOffset);
        }
        catch (Exception exception)
        {
            // unexpected exception type most likely due to exception in partition assigned handler.
            // need to recreate consumer, so don't retry batch
            exception.DoNotRetryBatch();

            throw;
        }
    }

    public void ThrowIfNeeded()
    {
        if (this._exception != null)
        {
            throw this._exception;
        }
    }
}