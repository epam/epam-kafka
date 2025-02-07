﻿// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

namespace Epam.Kafka.Internals;

internal sealed class SharedClient : ClientWrapper, IObservable<Error>, IObservable<string>, IObservable<Statistics>
{
    public const string ProducerName = "Shared";

#pragma warning disable CA2213 // See comments for Dispose() method.
    private readonly IClient _client;
#pragma warning restore CA2213

    public SharedClient(IKafkaFactory kafkaFactory, string? cluster)
    {
        if (kafkaFactory == null) throw new ArgumentNullException(nameof(kafkaFactory));

        ProducerConfig config = kafkaFactory.CreateProducerConfig(ProducerName);

        if (string.IsNullOrWhiteSpace(config.ClientId))
        {
            config.ClientId = $"Epam.Kafka.SharedClient.{cluster}";
        }

        this._client = kafkaFactory.CreateProducer<Null, Null>(config, cluster);
    }
#pragma warning disable CA2215
    public override void Dispose()
    {
        // Have to implement IDisposable because it required by interface IClient defined in external library. 
        // As a workaround we don't allow to dispose because this is shared client and it's lifetime should be equal to lifetime of factory. 
        // Instead of this method factory will invoke DisposeInternal() on own dispose.
    }
#pragma warning restore CA2215

    public void DisposeInternal()
    {
        base.Dispose();
        this._client.Dispose();
    }

    protected override IClient Inner
    {
        get
        {
            this.EnsureNotDisposed();
            return this._client;
        }
    }

    public IDisposable Subscribe(IObserver<Error> observer)
    {
        this.EnsureNotDisposed();
        return ((IObservable<Error>)this._client).Subscribe(observer);
    }

    public IDisposable Subscribe(IObserver<string> observer)
    {
        this.EnsureNotDisposed();
        return ((IObservable<string>)this._client).Subscribe(observer);
    }

    public IDisposable Subscribe(IObserver<Statistics> observer)
    {
        this.EnsureNotDisposed();
        return ((IObservable<Statistics>)this._client).Subscribe(observer);
    }
}