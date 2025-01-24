// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

namespace Epam.Kafka.Internals.Observable;

internal class ObservableConsumer<TKey, TValue> : ObservableClient, IConsumer<TKey, TValue>
{
    private readonly IConsumer<TKey, TValue> _inner;

    public ObservableConsumer(ConsumerBuilder<TKey, TValue> builder)
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
            this.StatJsonObservers = new List<IObserver<string>>();
        }
        catch (InvalidOperationException)
        {
            // stats handler already set
        }

        this._inner = builder.Build();
    }

    protected override IClient Inner => this._inner;

    public override void Dispose()
    {
        try
        {
            this._inner.Dispose();
        }
        finally
        {
            this.ClearObservers();
        }
    }

    public ConsumeResult<TKey, TValue> Consume(int millisecondsTimeout)
    {
        return this._inner.Consume(millisecondsTimeout);
    }

    public ConsumeResult<TKey, TValue> Consume(CancellationToken cancellationToken = new CancellationToken())
    {
        return this._inner.Consume(cancellationToken);
    }

    public ConsumeResult<TKey, TValue> Consume(TimeSpan timeout)
    {
        return this._inner.Consume(timeout);
    }

    public void Subscribe(IEnumerable<string> topics)
    {
        this._inner.Subscribe(topics);
    }

    public void Subscribe(string topic)
    {
        this._inner.Subscribe(topic);
    }

    public void Unsubscribe()
    {
        this._inner.Unsubscribe();
    }

    public void Assign(TopicPartition partition)
    {
        this._inner.Assign(partition);
    }

    public void Assign(TopicPartitionOffset partition)
    {
        this._inner.Assign(partition);
    }

    public void Assign(IEnumerable<TopicPartitionOffset> partitions)
    {
        this._inner.Assign(partitions);
    }

    public void Assign(IEnumerable<TopicPartition> partitions)
    {
        this._inner.Assign(partitions);
    }

    public void IncrementalAssign(IEnumerable<TopicPartitionOffset> partitions)
    {
        this._inner.IncrementalAssign(partitions);
    }

    public void IncrementalAssign(IEnumerable<TopicPartition> partitions)
    {
        this._inner.IncrementalAssign(partitions);
    }

    public void IncrementalUnassign(IEnumerable<TopicPartition> partitions)
    {
        this._inner.IncrementalUnassign(partitions);
    }

    public void Unassign()
    {
        this._inner.Unassign();
    }

    public void StoreOffset(ConsumeResult<TKey, TValue> result)
    {
        this._inner.StoreOffset(result);
    }

    public void StoreOffset(TopicPartitionOffset offset)
    {
        this._inner.StoreOffset(offset);
    }

    public List<TopicPartitionOffset> Commit()
    {
        return this._inner.Commit();
    }

    public void Commit(IEnumerable<TopicPartitionOffset> offsets)
    {
        this._inner.Commit(offsets);
    }

    public void Commit(ConsumeResult<TKey, TValue> result)
    {
        this._inner.Commit(result);
    }

    public void Seek(TopicPartitionOffset tpo)
    {
        this._inner.Seek(tpo);
    }

    public void Pause(IEnumerable<TopicPartition> partitions)
    {
        this._inner.Pause(partitions);
    }

    public void Resume(IEnumerable<TopicPartition> partitions)
    {
        this._inner.Resume(partitions);
    }

    public List<TopicPartitionOffset> Committed(TimeSpan timeout)
    {
        return this._inner.Committed(timeout);
    }

    public List<TopicPartitionOffset> Committed(IEnumerable<TopicPartition> partitions, TimeSpan timeout)
    {
        return this._inner.Committed(partitions, timeout);
    }

    public Offset Position(TopicPartition partition)
    {
        return this._inner.Position(partition);
    }

    public List<TopicPartitionOffset> OffsetsForTimes(IEnumerable<TopicPartitionTimestamp> timestampsToSearch, TimeSpan timeout)
    {
        return this._inner.OffsetsForTimes(timestampsToSearch, timeout);
    }

    public WatermarkOffsets GetWatermarkOffsets(TopicPartition topicPartition)
    {
        return this._inner.GetWatermarkOffsets(topicPartition);
    }

    public WatermarkOffsets QueryWatermarkOffsets(TopicPartition topicPartition, TimeSpan timeout)
    {
        return this._inner.QueryWatermarkOffsets(topicPartition, timeout);
    }

    public void Close()
    {
        this._inner.Close();
    }

    public string MemberId => this._inner.MemberId;

    public List<TopicPartition> Assignment => this._inner.Assignment;

    public List<string> Subscription => this._inner.Subscription;

    public IConsumerGroupMetadata ConsumerGroupMetadata => this._inner.ConsumerGroupMetadata;
}