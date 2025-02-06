// Copyright © 2024 EPAM Systems

namespace Epam.Kafka.Internals.Observable;

internal sealed class Unsubscriber<T> : IDisposable
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