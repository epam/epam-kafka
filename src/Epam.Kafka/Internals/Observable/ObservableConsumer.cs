// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.Metrics;

namespace Epam.Kafka.Internals.Observable;

internal class ObservableConsumer<TKey, TValue> : ObservableClient, IConsumer<TKey, TValue>
{
    private readonly IConsumer<TKey, TValue> _inner;

    public ObservableConsumer(ConsumerBuilder<TKey, TValue> builder, ConsumerConfig config)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));

        try
        {
            builder.SetErrorHandler((_, error) => this.ErrorHandler(error));
            this.ErrorObservers = new List<IObserver<Error>>();
        }
        catch (InvalidOperationException)
        {
            // errors handler already set
        }

        try
        {
            builder.SetStatisticsHandler((_, json) => this.StatisticsHandler(json));
            this.StatObservers = new List<IObserver<string>>();
#pragma warning disable CA2000 // unsubscribe not needed
            this.Subscribe(new ConsumerMetrics(config));
#pragma warning restore CA2000
        }
        catch (InvalidOperationException)
        {
            // stats handler already set
        }

        this._inner = builder.Build();
    }

    protected override IClient Inner
    {
        get
        {
            this.EnsureNotDisposed();
            return this._inner;
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        try
        {
            this._inner.Dispose();
        }
        finally
        {
            this.CompleteObservers();
        }
    }

    public ConsumeResult<TKey, TValue> Consume(int millisecondsTimeout)
    {
        this.EnsureNotDisposed();
        return this._inner.Consume(millisecondsTimeout);
    }

    public ConsumeResult<TKey, TValue> Consume(CancellationToken cancellationToken = new CancellationToken())
    {
        this.EnsureNotDisposed();
        return this._inner.Consume(cancellationToken);
    }

    public ConsumeResult<TKey, TValue> Consume(TimeSpan timeout)
    {
        this.EnsureNotDisposed();
        return this._inner.Consume(timeout);
    }

    public void Subscribe(IEnumerable<string> topics)
    {
        this.EnsureNotDisposed();
        this._inner.Subscribe(topics);
    }

    public void Subscribe(string topic)
    {
        this.EnsureNotDisposed();
        this._inner.Subscribe(topic);
    }

    public void Unsubscribe()
    {
        this.EnsureNotDisposed();
        this._inner.Unsubscribe();
    }

    public void Assign(TopicPartition partition)
    {
        this.EnsureNotDisposed();
        this._inner.Assign(partition);
    }

    public void Assign(TopicPartitionOffset partition)
    {
        this.EnsureNotDisposed();
        this._inner.Assign(partition);
    }

    public void Assign(IEnumerable<TopicPartitionOffset> partitions)
    {
        this.EnsureNotDisposed();
        this._inner.Assign(partitions);
    }

    public void Assign(IEnumerable<TopicPartition> partitions)
    {
        this.EnsureNotDisposed();
        this._inner.Assign(partitions);
    }

    public void IncrementalAssign(IEnumerable<TopicPartitionOffset> partitions)
    {
        this.EnsureNotDisposed();
        this._inner.IncrementalAssign(partitions);
    }

    public void IncrementalAssign(IEnumerable<TopicPartition> partitions)
    {
        this.EnsureNotDisposed();
        this._inner.IncrementalAssign(partitions);
    }

    public void IncrementalUnassign(IEnumerable<TopicPartition> partitions)
    {
        this.EnsureNotDisposed();
        this._inner.IncrementalUnassign(partitions);
    }

    public void Unassign()
    {
        this.EnsureNotDisposed();
        this._inner.Unassign();
    }

    public void StoreOffset(ConsumeResult<TKey, TValue> result)
    {
        this.EnsureNotDisposed();
        this._inner.StoreOffset(result);
    }

    public void StoreOffset(TopicPartitionOffset offset)
    {
        this.EnsureNotDisposed();
        this._inner.StoreOffset(offset);
    }

    public List<TopicPartitionOffset> Commit()
    {
        this.EnsureNotDisposed();
        return this._inner.Commit();
    }

    public void Commit(IEnumerable<TopicPartitionOffset> offsets)
    {
        this.EnsureNotDisposed();
        this._inner.Commit(offsets);
    }

    public void Commit(ConsumeResult<TKey, TValue> result)
    {
        this.EnsureNotDisposed();
        this._inner.Commit(result);
    }

    public void Seek(TopicPartitionOffset tpo)
    {
        this.EnsureNotDisposed();
        this._inner.Seek(tpo);
    }

    public void Pause(IEnumerable<TopicPartition> partitions)
    {
        this.EnsureNotDisposed();
        this._inner.Pause(partitions);
    }

    public void Resume(IEnumerable<TopicPartition> partitions)
    {
        this.EnsureNotDisposed();
        this._inner.Resume(partitions);
    }

    public List<TopicPartitionOffset> Committed(TimeSpan timeout)
    {
        this.EnsureNotDisposed();
        return this._inner.Committed(timeout);
    }

    public List<TopicPartitionOffset> Committed(IEnumerable<TopicPartition> partitions, TimeSpan timeout)
    {
        this.EnsureNotDisposed();
        return this._inner.Committed(partitions, timeout);
    }

    public Offset Position(TopicPartition partition)
    {
        this.EnsureNotDisposed();
        return this._inner.Position(partition);
    }

    public List<TopicPartitionOffset> OffsetsForTimes(IEnumerable<TopicPartitionTimestamp> timestampsToSearch, TimeSpan timeout)
    {
        this.EnsureNotDisposed();
        return this._inner.OffsetsForTimes(timestampsToSearch, timeout);
    }

    public WatermarkOffsets GetWatermarkOffsets(TopicPartition topicPartition)
    {
        this.EnsureNotDisposed();
        return this._inner.GetWatermarkOffsets(topicPartition);
    }

    public WatermarkOffsets QueryWatermarkOffsets(TopicPartition topicPartition, TimeSpan timeout)
    {
        this.EnsureNotDisposed();
        return this._inner.QueryWatermarkOffsets(topicPartition, timeout);
    }

    public void Close()
    {
        this.EnsureNotDisposed();
        this._inner.Close();
    }

    public string MemberId
    {
        get
        {
            this.EnsureNotDisposed();
            return this._inner.MemberId;
        }
    }

    public List<TopicPartition> Assignment
    {
        get
        {
            this.EnsureNotDisposed();
            return this._inner.Assignment;
        }
    }

    public List<string> Subscription
    {
        get
        {
            this.EnsureNotDisposed();
            return this._inner.Subscription;
        }
    }

    public IConsumerGroupMetadata ConsumerGroupMetadata
    {
        get
        {
            this.EnsureNotDisposed();
            return this._inner.ConsumerGroupMetadata;
        }
    }
}