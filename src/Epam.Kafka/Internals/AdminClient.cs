// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

namespace Epam.Kafka.Internals;

internal sealed class AdminClient : IClient
{
    private readonly IClient _client;

    public AdminClient(IClient client)
    {
        this._client = client ?? throw new ArgumentNullException(nameof(client));
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
        this._client.Dispose();
    }
}