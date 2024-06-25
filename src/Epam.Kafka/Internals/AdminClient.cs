// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

namespace Epam.Kafka.Internals;

internal sealed class AdminClient : IObservable<Error>, IClient
{
#pragma warning disable CA2213 // See comments for Dispose() method.
    private readonly IClient _client;
#pragma warning restore CA2213
    private readonly List<IObserver<Error>> _observers = new();

    public AdminClient(IKafkaFactory kafkaFactory, ProducerConfig config, string? cluster)
    {
        if (kafkaFactory == null) throw new ArgumentNullException(nameof(kafkaFactory));
        if (config == null) throw new ArgumentNullException(nameof(config));

        this._client = kafkaFactory.CreateProducer<Null, Null>(config, cluster,builder => builder.SetErrorHandler(this.ErrorHandler));
    }

    private void ErrorHandler(IProducer<Null, Null> producer, Error error)
    {
        foreach (IObserver<Error> observer in this._observers)
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
            foreach (var item in this._observers.ToArray())
            {
                if (this._observers.Contains(item))
                {
                    item.OnCompleted();
                }
            }

            this._observers.Clear();
        }
    }

    public IDisposable Subscribe(IObserver<Error> observer)
    {
        if (!this._observers.Contains(observer))
        {
            this._observers.Add(observer);
        }

        return new Unsubscriber(this._observers, observer);
    }

    private class Unsubscriber : IDisposable
    {
        private readonly List<IObserver<Error>> _observers;
        private readonly IObserver<Error> _observer;

        public Unsubscriber(List<IObserver<Error>> observers, IObserver<Error> observer)
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