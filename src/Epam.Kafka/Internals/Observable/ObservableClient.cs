// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

namespace Epam.Kafka.Internals.Observable;

internal abstract class ObservableClient : ClientWrapper, IObservable<Error>, IObservable<string>
{
    protected List<IObserver<Error>>? ErrorObservers { get; set; }
    protected List<IObserver<string>>? StatObservers { get; set; }

    protected void StatisticsHandler(string json)
    {
        foreach (IObserver<string> observer in this.StatObservers!)
        {
            observer.OnNext(json);
        }
    }

    protected void ErrorHandler(Error error)
    {
        foreach (IObserver<Error> observer in this.ErrorObservers!)
        {
            observer.OnNext(error);
        }
    }

    protected void ClearObservers()
    {
        ClearObservers(this.ErrorObservers);
        ClearObservers(this.StatObservers);
    }

    private static void ClearObservers<T>(List<IObserver<T>>? items)
    {
        if (items == null)
        {
            return;
        }

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
        if (this.ErrorObservers == null)
        {
            throw new InvalidOperationException(
                "Cannot subscribe to errors because handler was explicitly set in producer/consumer builder.");
        }

        if (!this.ErrorObservers.Contains(observer))
        {
            this.ErrorObservers.Add(observer);
        }

        return new Unsubscriber<Error>(this.ErrorObservers, observer);
    }

    public IDisposable Subscribe(IObserver<string> observer)
    {
        if (this.StatObservers == null)
        {
            throw new InvalidOperationException(
                "Cannot subscribe to statistics because handler was explicitly set in producer/consumer builder.");
        }

        if (!this.StatObservers.Contains(observer))
        {
            this.StatObservers.Add(observer);
        }

        return new Unsubscriber<string>(this.StatObservers, observer);
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