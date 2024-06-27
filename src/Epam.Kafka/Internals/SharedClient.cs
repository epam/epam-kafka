// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka.Internals.Observable;

namespace Epam.Kafka.Internals;

internal sealed class SharedClient : ObservableClient, IClient
{
    public const string ProducerName = "Shared";

#pragma warning disable CA2213 // See comments for Dispose() method.
    private readonly IClient _client;
#pragma warning restore CA2213


    public SharedClient(IKafkaFactory kafkaFactory, string? cluster)
    {
        if (kafkaFactory == null) throw new ArgumentNullException(nameof(kafkaFactory));

        ProducerConfig config = kafkaFactory.CreateProducerConfig(ProducerName);

        this._client = kafkaFactory.CreateProducer<Null, Null>(config, cluster, builder =>
        {
            builder.SetErrorHandler((_, error) => this.ErrorHandler(error));
            builder.SetStatisticsHandler((_, json) => this.StatisticsHandler(json));
        });

        this.ErrorObservers = new List<IObserver<Error>>();
        this.StatObservers = new List<IObserver<Statistics>>();
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
            this.ClearObservers();
        }
    }
}