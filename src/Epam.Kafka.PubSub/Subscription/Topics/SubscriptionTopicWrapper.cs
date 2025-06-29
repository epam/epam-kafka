﻿// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Subscription.Options;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.PubSub.Subscription.State;
using Epam.Kafka.PubSub.Utils;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Epam.Kafka.PubSub.Subscription.Topics;

internal sealed class SubscriptionTopicWrapper<TKey, TValue> : IDisposable
{
    private readonly List<ConsumeResult<TKey, TValue>> _buffer;

    private Exception? _exception;

    private readonly AutoOffsetReset? _autoOffsetReset;

    private readonly HashSet<TopicPartition> _paused = new();

    private readonly int _consumeTimeoutMs;
    private readonly bool _rebalance;

    private readonly HashSet<TopicPartition> _newPartitions = new();
    private readonly Dictionary<TopicPartition, Offset> _offsets = new();

    public SubscriptionTopicWrapper(IKafkaFactory kafkaFactory,
        SubscriptionMonitor monitor,
        IOptionsMonitor<SubscriptionOptions> optionsMonitor,
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

        this._rebalance = this.Options.StateType.Name == typeof(CombinedState<>).Name ||
                          this.Options.StateType == typeof(InternalKafkaState);

        ConsumerConfig config = kafkaFactory.CreateConsumerConfig(options.Consumer);

        config = config.Clone(this.Monitor.NamePlaceholder);

        this.ConfigureConsumerConfig(config, optionsMonitor);

        this._autoOffsetReset = config.AutoOffsetReset;
        this._consumeTimeoutMs = config.GetCancellationDelayMaxMs();
        this.ConsumerGroup = config.GroupId;

        if (!monitor.TryRegisterGroupId(config, this.Options, out string? msg))
        {
            var exception = new InvalidOperationException($"Unable to use '{config.GroupId}' group.id in '{monitor.Name}' subscription. {msg}");
            exception.DoNotRetryBatch();

            throw exception;
        }

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

        // Warmup consumer to avoid potential issues with OAuth handler.
        // Consumer just created, so not assigned to any partition.
        this.Consumer.Consume(100);
    }

    public SubscriptionMonitor Monitor { get; }
    public SubscriptionOptions Options { get; }
    public ILogger Logger { get; }

    public IConsumer<TKey, TValue> Consumer { get; }
    public string ConsumerGroup { get; }
    public bool UnassignedBeforeRead { get; private set; }

    public Func<IReadOnlyCollection<TopicPartition>, IReadOnlyCollection<TopicPartitionOffset>>? ExternalState { get; set; }

    public bool TryGetOffset(TopicPartition tp, out Offset result)
    {
        return this._offsets.TryGetValue(tp, out result);
    }

    public void OnAssign(IReadOnlyCollection<TopicPartitionOffset> items)
    {
        if (items.Count > 0)
        {
            this.Consumer.Assign(items);

            foreach (TopicPartitionOffset tpo in items)
            {
                this._offsets[tpo.TopicPartition] = tpo.Offset;
            }

            this.Logger.PartitionsAssigned(this.Monitor.Name, null, items);
        }
    }

    private void CheckRebalance()
    {
        // ensure to consume at least one message
        // to trigger potential rebalance
        if (this._rebalance && this._buffer.Count >= this.Options.BatchSize)
        {
            ConsumeResult<TKey, TValue> last = this._buffer.Last();

            this.Consumer.Seek(last.TopicPartitionOffset);

            this.CleanupBuffer(x => x.TopicPartitionOffset == last.TopicPartitionOffset, "check rebalance");
        }
    }

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

    public IReadOnlyCollection<TopicPartitionOffset> GetAndResetState(
        IExternalOffsetsStorage storage,
        IReadOnlyCollection<TopicPartition> topicPartitions,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TopicPartitionOffset> state =
            storage.GetOrCreate(topicPartitions, this.ConsumerGroup, cancellationToken);

        if (state.Any(x => x.Offset == Offset.Unset))
        {
            state = this.AutoResetOffsets(state, out List<TopicPartitionOffset>? toCommit);

            if (toCommit.Count > 0)
            {
                storage.CommitOrReset(toCommit, this.ConsumerGroup, cancellationToken);
            }
        }

        return state;
    }

    private List<TopicPartitionOffset> AutoResetOffsets(IReadOnlyCollection<TopicPartitionOffset> offsets, out List<TopicPartitionOffset> toReset)
    {
        toReset = new List<TopicPartitionOffset>(offsets.Count);
        List<TopicPartitionOffset> result = new(offsets.Count);

        foreach (TopicPartitionOffset tpo in offsets)
        {
            if (tpo.Offset == Offset.Unset)
            {
                TopicPartition topicPartition = tpo.TopicPartition;

                WatermarkOffsets q = this.Consumer.QueryWatermarkOffsets(topicPartition, TimeSpan.FromSeconds(5));

                switch (this._autoOffsetReset)
                {
                    case AutoOffsetReset.Earliest:
                        var low = new TopicPartitionOffset(topicPartition, q.Low);
                        toReset.Add(low);
                        result.Add(low);
                        break;
                    case AutoOffsetReset.Latest:
                        var high = new TopicPartitionOffset(topicPartition, q.High);
                        toReset.Add(high);
                        result.Add(high);
                        break;
                    case AutoOffsetReset.Error:
                        throw new KafkaException(ErrorCode.Local_NoOffset);

                    default: result.Add(tpo); break;
                }
            }
            else
            {
                result.Add(tpo);
            }
        }

        return result;
    }

    public void ClearIfNotAssigned()
    {
        if (this.Consumer.Assignment.Count == 0 && this.Consumer.Subscription.Count == 0)
        {
            this._buffer.Clear();
            this._offsets.Clear();
        }
    }

    public IReadOnlyCollection<ConsumeResult<TKey, TValue>> GetBatch(ActivityWrapper activitySpan,
        CancellationToken cancellationToken)
    {
        if (this._buffer.Count == 0 || this._rebalance)
        {
            this.CheckRebalance();

            int initialCount = this._buffer.Count;

            using ActivityWrapper wrapper = activitySpan.CreateSpan("read");

            this.ReadToBuffer(activitySpan, cancellationToken);

            // try to read again because new partitions were assigned, so we can have messages 
            if (this._buffer.Count == 0 && this._newPartitions.Count > 0)
            {
                this.ReadToBuffer(activitySpan, cancellationToken);
            }

            wrapper.SetResult(this._buffer.Count - initialCount);
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

                this._offsets[item.TopicPartition] = item.Offset;
            }
        }
    }

    public void OnReset(IReadOnlyCollection<TopicPartitionOffset> items)
    {
        if (items.Count > 0)
        {
            List<TopicPartitionOffset> reset = new(items.Count);
            List<TopicPartitionOffset> resume = new(items.Count);

            foreach (TopicPartitionOffset tpo in items)
            {
                if (this._paused.Remove(tpo.TopicPartition))
                {
                    resume.Add(tpo);
                }
                else
                {
                    reset.Add(tpo);
                }
            }

            if (reset.Count > 0)
            {
                this.Logger.OffsetsReset(this.Monitor.Name, reset);
                this.CleanupBuffer(x => reset.Any(v => v.TopicPartition == x.TopicPartition), "partition offset reset");
            }

            if (resume.Count > 0)
            {
                this.Consumer.Resume(resume.Select(x => x.TopicPartition));
                this.Logger.PartitionsResumed(this.Monitor.Name, resume);
                this.CleanupBuffer(x => resume.Any(v => v.TopicPartition == x.TopicPartition), "partition offset resume");
            }
        }
    }

    public bool OnPause(IReadOnlyCollection<TopicPartition> items)
    {
        return items.Count > 0 && this.OnPauseEnumerate(items);
    }

    private bool OnPauseEnumerate(IEnumerable<TopicPartition> items)
    {
        List<TopicPartition> result = new();

        foreach (TopicPartition tp in items)
        {
            if (this.Consumer.Assignment.Contains(tp) && !this._paused.Contains(tp))
            {
                result.Add(tp);
            }
        }

        if (result.Count > 0)
        {
            try
            {
                this.Consumer.Pause(result);
            }
            catch (Exception e)
            {
                e.DoNotRetryBatch();
                throw;
            }

            foreach (TopicPartition r in result)
            {
                this._paused.Add(r);
                this._offsets[r] = ExternalOffset.Paused;
            }

            this.Logger.PartitionsPaused(this.Monitor.Name, result);

            return this.CleanupBuffer(x => result.Any(v => v == x.TopicPartition), "partition paused");
        }

        return false;
    }

    private void ConfigureConsumerBuilder(ConsumerBuilder<TKey, TValue> builder)
    {
        builder.SetPartitionsAssignedHandler((consumer, list) =>
        {
            var result = new List<TopicPartitionOffset>(list.Count);
            result.AddRange(list.Select(x =>
            {
                this._newPartitions.Add(x);
                return new TopicPartitionOffset(x, Offset.Unset);
            }));

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
                    this._offsets.Remove(partitionOffset.TopicPartition);
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
                    this._offsets.Remove(partitionOffset.TopicPartition);
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
                    IEnumerable<TopicPartitionOffset> state = this.ExternalState.Invoke(tp);

                    foreach (TopicPartitionOffset tpo in state)
                    {
                        this._offsets[tpo.TopicPartition] = tpo.Offset;

                        list.Add(tpo.Offset == ExternalOffset.Paused
                            ? new TopicPartitionOffset(tpo.TopicPartition, Offset.End)
                            : tpo);
                    }
                }
                catch (Exception exception)
                {
                    // Save it and throw later to trigger pipeline retry.
                    exception.DoNotRetryBatch();
                    this._exception = exception;

                    if (exception is not OperationCanceledException)
                    {
                        this.Logger.PartitionsAssignError(exception, this.Monitor.Name, c.MemberId, tp);
                    }
                    else
                    {
                        this.Logger.PartitionsAssignCancelled(this.Monitor.Name, c.MemberId, tp);
                    }

                    // set Offset.End special value to prevent reading from topic partitions until pipeline restart.
                    list.AddRange(tp.Select(x => new TopicPartitionOffset(x, Offset.End)));

                    return;
                }
#pragma warning restore CA1031
            }

            this.Logger.PartitionsAssigned(this.Monitor.Name, c.MemberId, list);
        }
    }

    private bool CleanupBuffer(Predicate<ConsumeResult<TKey, TValue>> predicate, string reason)
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

        return count > 0;
    }

    private void ConfigureConsumerConfig(ConsumerConfig config, IOptionsMonitor<SubscriptionOptions> optionsMonitor)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        config.EnableAutoCommit = false;
        config.EnableAutoOffsetStore = false;

        if (config.All(x => x.Key != KafkaConfigExtensions.DotnetLoggerCategoryKey))
        {
            config.SetDotnetLoggerCategory(this.Monitor.FullName);
        }

        // to avoid leaving group in case of long-running processing
        if (!config.MaxPollIntervalMs.HasValue && this._rebalance)
        {
            TimeSpan handlerTimeout = this.CalculateMaxHandlerTimeout(optionsMonitor);

            // guard against extremely large values
            int mpi = (int)Math.Min(handlerTimeout.TotalMilliseconds, TimeSpan.FromHours(3).TotalMilliseconds);

            config.MaxPollIntervalMs = mpi;
        }
    }

    private TimeSpan CalculateMaxHandlerTimeout(IOptionsMonitor<SubscriptionOptions> optionsMonitor)
    {
        TimeSpan result = this.Options.HandlerTimeout;

        if (this.Options.HandlerConcurrencyGroup.HasValue)
        {
            foreach (SubscriptionMonitor sm in this.Monitor.Context.Subscriptions.Values.Where(x => x.FullName != this.Monitor.FullName))
            {
                try
                {
                    SubscriptionOptions so = optionsMonitor.Get(sm.Name);

                    if (so.Enabled && so.HandlerConcurrencyGroup == this.Options.HandlerConcurrencyGroup)
                    {
                        result += so.HandlerTimeout;
                    }
                }
#pragma warning disable CA1031 // don't fail if not possible to create options for other subscriptions
                catch (Exception e)
                {
                    this.Logger.PollIntervalIgnoreOptions(e, this.Monitor.Name, sm.Name,
                        this.Options.HandlerConcurrencyGroup.Value);
                }
#pragma warning restore CA1031
            }
        }

        if (this.Options.BatchRetryCount > 0)
        {
            // potential rebalance may happen only after retry attempt
            result += this.Options.BatchRetryMaxTimeout;
        }

        if (this.Options.BatchNotAssignedTimeout > result)
        {
            result = this.Options.BatchNotAssignedTimeout;
        }

        if (this.Options.BatchPausedTimeout > result)
        {
            result = this.Options.BatchPausedTimeout;
        }

        if (this.Options.BatchEmptyTimeout > result)
        {
            result = this.Options.BatchEmptyTimeout;
        }

        // time buffer for external state handling
        return result.Add(TimeSpan.FromMinutes(2));
    }

    public void CommitOffsets(ActivityWrapper activitySpan, IReadOnlyCollection<TopicPartitionOffset> offsets)
    {
        if (offsets.Count > 0)
        {
            using ActivityWrapper span = activitySpan.CreateSpan("commit_kafka");
            // commit to kafka also
            this.Consumer.Commit(offsets);

            span.SetResult(offsets);
        }
    }

    private void ReadToBuffer(ActivityWrapper span, CancellationToken cancellationToken)
    {
        this.UnassignedBeforeRead = this.Consumer.Assignment.Count == 0;

        this._newPartitions.Clear();

        int batchSize = this.Options.BatchSize;

        try
        {
            while (!cancellationToken.IsCancellationRequested && this._buffer.Count < batchSize)
            {
                ConsumeResult<TKey, TValue>? consumeResult = this.Consumer.Consume(this._consumeTimeoutMs);

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

#pragma warning disable IDE0010 // Add missing cases
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
#pragma warning restore IDE0010 // Add missing cases

            // unable to return at least something, so only throw
            if (this._buffer.Count == 0)
            {
                this.HandleNewPartitions(span);

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

        this.HandleNewPartitions(span);
    }

    private void HandleNewPartitions(ActivityWrapper span)
    {
        if (this._newPartitions.Count > 0)
        {
            // commit offset for new partitions in case of combined state
            if (this.Options.StateType != typeof(InternalKafkaState))
            {
                this.CommitOffsetIfNeeded(span,
                    this._offsets.Where(x => this._newPartitions.Contains(x.Key))
                        .Select(x => new TopicPartitionOffset(x.Key, x.Value)));
            }

            // pause consumer for new partitions with special offset
            this.OnPause(this._offsets
                .Where(x => this._newPartitions.Contains(x.Key) && x.Value == ExternalOffset.Paused)
                .Select(x => x.Key).ToArray());
        }
    }

    public void Seek(TopicPartitionOffset tpo)
    {
        this.Consumer.Seek(tpo);
        this._offsets[tpo.TopicPartition] = tpo.Offset;
    }

    public void ThrowIfNeeded()
    {
        if (this._exception != null)
        {
            throw this._exception;
        }
    }
}