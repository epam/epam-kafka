// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.Internals.Metrics;

using System.Diagnostics.Metrics;

namespace Epam.Kafka.Internals.Observable;

internal class ObservableProducer<TKey, TValue> : ObservableClient, IProducer<TKey, TValue>
{
    private readonly IProducer<TKey, TValue> _inner;
    private readonly Meter? _meter;

    public ObservableProducer(ProducerBuilder<TKey, TValue> builder, bool metrics)
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
            this.StatObservers = new List<IObserver<Statistics>>();
            if (metrics)
            {
                this._meter = new Meter(Statistics.MeterName);
                this.StatObservers.Add(new ProducerMetrics(this._meter));
            }
        }
        catch (InvalidOperationException)
        {
            // stats handler already set
        }

        this._inner = builder.Build();
    }

    public void Dispose()
    {
        try
        {
            this._inner.Dispose();
        }
        finally
        {
            this.ClearObservers();

            this._meter?.Dispose();
        }
    }

    public int AddBrokers(string brokers)
    {
        return this._inner.AddBrokers(brokers);
    }

    public void SetSaslCredentials(string username, string password)
    {
        this._inner.SetSaslCredentials(username, password);
    }

    public Handle Handle => this._inner.Handle;

    public string Name => this._inner.Name;

    public Task<DeliveryResult<TKey, TValue>> ProduceAsync(string topic, Message<TKey, TValue> message, CancellationToken cancellationToken = new CancellationToken())
    {
        return this._inner.ProduceAsync(topic, message, cancellationToken);
    }

    public Task<DeliveryResult<TKey, TValue>> ProduceAsync(TopicPartition topicPartition, Message<TKey, TValue> message,
        CancellationToken cancellationToken = new CancellationToken())
    {
        return this._inner.ProduceAsync(topicPartition, message, cancellationToken);
    }

    public void Produce(string topic, Message<TKey, TValue> message, Action<DeliveryReport<TKey, TValue>>? deliveryHandler = null)
    {
        this._inner.Produce(topic, message, deliveryHandler);
    }

    public void Produce(TopicPartition topicPartition, Message<TKey, TValue> message, Action<DeliveryReport<TKey, TValue>>? deliveryHandler = null)
    {
        this._inner.Produce(topicPartition, message, deliveryHandler);
    }

    public int Poll(TimeSpan timeout)
    {
        return this._inner.Poll(timeout);
    }

    public int Flush(TimeSpan timeout)
    {
        return this._inner.Flush(timeout);
    }

    public void Flush(CancellationToken cancellationToken = new CancellationToken())
    {
        this._inner.Flush(cancellationToken);
    }

    public void InitTransactions(TimeSpan timeout)
    {
        this._inner.InitTransactions(timeout);
    }

    public void BeginTransaction()
    {
        this._inner.BeginTransaction();
    }

    public void CommitTransaction(TimeSpan timeout)
    {
        this._inner.CommitTransaction(timeout);
    }

    public void CommitTransaction()
    {
        this._inner.CommitTransaction();
    }

    public void AbortTransaction(TimeSpan timeout)
    {
        this._inner.AbortTransaction(timeout);
    }

    public void AbortTransaction()
    {
        this._inner.AbortTransaction();
    }

    public void SendOffsetsToTransaction(IEnumerable<TopicPartitionOffset> offsets, IConsumerGroupMetadata groupMetadata, TimeSpan timeout)
    {
        this._inner.SendOffsetsToTransaction(offsets, groupMetadata, timeout);
    }
}