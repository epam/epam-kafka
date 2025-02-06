// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.Stats;

namespace Epam.Kafka.Internals.Observable;

#pragma warning disable CA1031 // notify other listeners even if one of them failed

internal abstract class ObservableClient : ClientWrapper, IObservable<Error>, IObservable<string>, IObservable<Statistics>
{
    private readonly ParseStatsJsonObserver _parseObserver = new();

    protected List<IObserver<Error>>? ErrorObservers { get; set; }
    protected List<IObserver<string>>? StatObservers { get; set; }

    protected void StatisticsHandler(string json)
    {
        foreach (IObserver<string> observer in this.StatObservers!)
        {
            try
            {
                observer.OnNext(json);
            }
            catch
            {
                // notify other listeners even if one of them failed
            }
        }
    }

    protected void ErrorHandler(Error error)
    {
        foreach (IObserver<Error> observer in this.ErrorObservers!)
        {
            try
            {
                observer.OnNext(error);
            }
            catch
            {
                // notify other listeners even if one of them failed
            }
        }
    }

    protected void CompleteObservers()
    {
        CompleteObservers(this.ErrorObservers);
        CompleteObservers(this.StatObservers);
    }

    private static void CompleteObservers<T>(List<IObserver<T>>? items)
    {
        if (items == null)
        {
            return;
        }

        foreach (IObserver<T> item in items)
        {
            try
            {
                item.OnCompleted();
            }
            catch
            {
                // notify other listeners even if one of them failed
            }
        }
    }

    public IDisposable Subscribe(IObserver<Error> observer)
    {
        if (observer == null) throw new ArgumentNullException(nameof(observer));

        this.EnsureNotDisposed();

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
        if (observer == null) throw new ArgumentNullException(nameof(observer));

        this.EnsureNotDisposed();

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

    public IDisposable Subscribe(IObserver<Statistics> observer)
    {
        if (observer == null) throw new ArgumentNullException(nameof(observer));

        this.EnsureNotDisposed();

#pragma warning disable CA2000 // don't need to unsubscribe
        this.Subscribe(this._parseObserver);
#pragma warning restore CA2000
        return this._parseObserver.Subscribe(observer);
    }
}

#pragma warning restore CA1031