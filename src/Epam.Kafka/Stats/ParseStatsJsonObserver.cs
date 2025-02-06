// Copyright © 2024 EPAM Systems

using Epam.Kafka.Internals.Observable;

namespace Epam.Kafka.Stats;

#pragma warning disable CA1031 // notify other listeners even if one of them failed

internal sealed class ParseStatsJsonObserver : IObserver<string>, IObservable<Statistics>
{
    private readonly List<IObserver<Statistics>> _inner = new();

    public void OnNext(string value)
    {
        Statistics statistics;
        try
        {
            statistics = Statistics.FromJson(value);
        }
        catch (Exception e)
        {
            this.OnError(e);

            throw;
        }

        foreach (IObserver<Statistics> observer in this._inner)
        {
            try
            {
                observer.OnNext(statistics);
            }
            catch
            {
                // notify other listeners even if one of them failed
            }
        }
    }

    public void OnError(Exception error)
    {
        foreach (IObserver<Statistics> observer in this._inner)
        {
            try
            {
                observer.OnError(error);
            }
            catch
            {
                // notify other listeners even if one of them failed
            }
        }
    }

    public void OnCompleted()
    {
        foreach (IObserver<Statistics> observer in this._inner)
        {
            try
            {
                observer.OnCompleted();
            }
            catch
            {
                // notify other listeners even if one of them failed
            }
        }

        this._inner.Clear();
    }

    public IDisposable Subscribe(IObserver<Statistics> observer)
    {
        if (!this._inner.Contains(observer))
        {
            this._inner.Add(observer);
        }

        return new Unsubscriber<Statistics>(this._inner, observer);
    }
}

#pragma warning restore CA1031