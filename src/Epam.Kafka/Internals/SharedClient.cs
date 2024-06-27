// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

namespace Epam.Kafka.Internals;

internal sealed class SharedClient : ISharedClient
{
    public const string ProducerName = "Shared";

#pragma warning disable CA2213 // See comments for Dispose() method.
    private readonly IClient _client;
#pragma warning restore CA2213
    private readonly List<IObserver<Error>> _errorObservers = new();
    private readonly List<IObserver<Statistics>> _statObservers = new();

    public SharedClient(IKafkaFactory kafkaFactory, string? cluster)
    {
        if (kafkaFactory == null) throw new ArgumentNullException(nameof(kafkaFactory));

        ProducerConfig config = kafkaFactory.CreateProducerConfig(ProducerName);

        this._client = kafkaFactory.CreateProducer<Null, Null>(config, cluster, builder =>
        {
            builder.SetErrorHandler(this.ErrorHandler);
            builder.SetStatisticsHandler(this.StatisticsHandler);
        });
    }

    private void StatisticsHandler(IProducer<Null, Null> producer, string json)
    {
        // don't try to parse if no subscribers
        if (this._statObservers.Count <= 0)
        {
            return;
        }

        Statistics value;

        try
        {
            value = Statistics.FromJson(json);
        }
        catch (Exception e) when (e is ArgumentNullException or ArgumentException)
        {
            foreach (IObserver<Statistics> observer in this._statObservers)
            {
                observer.OnError(e);
            }

            return;
        }

        foreach (IObserver<Statistics> observer in this._statObservers)
        {
            observer.OnNext(value);
        }
    }

    private void ErrorHandler(IProducer<Null, Null> producer, Error error)
    {
        foreach (IObserver<Error> observer in this._errorObservers)
        {
            observer.OnNext(error);
        }
    }

    public void Dispose()
    {
        // Have to implement IDisposable because it required by interface IClient defined in external library. 
        // As a workaround we don't allow to dispose because this is shared client and it's lifetime should be equal to lifetime of factory. 
        // Instead of this method factory will invoke DisposeInternal() on own dispose.
    }

    public int AddBrokers(string brokers)
    {
        return this._client.AddBrokers(brokers);
    }

    public void SetSaslCredentials(string username, string password)
    {
        this._client.SetSaslCredentials(username, password);
    }

    public Handle Handle => this._client.Handle;

    public string Name => this._client.Name;

    public void DisposeInternal()
    {
        try
        {
            this._client.Dispose();
        }
        finally
        {
            ClearObservers(this._errorObservers);
            ClearObservers(this._statObservers);
        }
    }

    private static void ClearObservers<T>(List<IObserver<T>> items)
    {
        foreach (IObserver<T> item in items.ToArray())
        {
            if (items.Contains(item))
            {
                item.OnCompleted();
            }
        }

        items.Clear();
    }

    public IDisposable Subscribe(IObserver<Error> observer)
    {
        if (!this._errorObservers.Contains(observer))
        {
            this._errorObservers.Add(observer);
        }

        return new Unsubscriber<Error>(this._errorObservers, observer);
    }

    public IDisposable Subscribe(IObserver<Statistics> observer)
    {
        if (!this._statObservers.Contains(observer))
        {
            this._statObservers.Add(observer);
        }

        return new Unsubscriber<Statistics>(this._statObservers, observer);
    }

    private class Unsubscriber<T> : IDisposable
    {
        private readonly List<IObserver<T>> _observers;
        private readonly IObserver<T> _observer;

        public Unsubscriber(List<IObserver<T>> observers, IObserver<T> observer)
        {
            this._observers = observers;
            this._observer = observer;
        }

        public void Dispose()
        {
            this._observers.Remove(this._observer);
        }
    }
}