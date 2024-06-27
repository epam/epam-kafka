// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

namespace Epam.Kafka.Internals.Observable;

internal abstract class ObservableClient : IObservable<Error>, IObservable<Statistics>
{
    protected List<IObserver<Error>>? ErrorObservers { get; set; }
    protected List<IObserver<Statistics>>? StatObservers { get; set; }

    protected void StatisticsHandler(string json)
    {
        // don't try to parse if no subscribers
        if (this.StatObservers!.Count <= 0)
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
            foreach (IObserver<Statistics> observer in this.StatObservers)
            {
                observer.OnError(e);
            }

            return;
        }

        foreach (IObserver<Statistics> observer in this.StatObservers)
        {
            observer.OnNext(value);
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

    public IDisposable Subscribe(IObserver<Statistics> observer)
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

        return new Unsubscriber<Statistics>(this.StatObservers, observer);
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