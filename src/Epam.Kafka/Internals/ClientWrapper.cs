// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

namespace Epam.Kafka.Internals;

internal abstract class ClientWrapper : IClient
{
    private bool _disposed;
    protected abstract IClient Inner { get; }

    public virtual void Dispose()
    {
        this._disposed = true;
    }

    protected void EnsureNotDisposed()
    {
        if (this._disposed)
        {
            throw new ObjectDisposedException(this.GetType().Name);
        }
    }

    public int AddBrokers(string brokers)
    {
        return this.Inner.AddBrokers(brokers);
    }

    public void SetSaslCredentials(string username, string password)
    {
        this.Inner.SetSaslCredentials(username, password);
    }

    public Handle Handle => this.Inner.Handle;

    public string Name => this.Inner.Name;
}