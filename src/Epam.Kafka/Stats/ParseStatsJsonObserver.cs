// Copyright © 2024 EPAM Systems

namespace Epam.Kafka.Stats;

internal sealed class ParseStatsJsonObserver : IObserver<string>
{
    private readonly IObserver<Statistics> _inner;

    public ParseStatsJsonObserver(IObserver<Statistics> inner)
    {
        this._inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public void OnNext(string value)
    {
        Statistics statistics;
        try
        {
            statistics = Statistics.FromJson(value);
        }
        catch (Exception e)
        {
            this._inner.OnError(e);

            throw;
        }

        this._inner.OnNext(statistics);
    }

    public void OnError(Exception error)
    {
        this._inner.OnError(error);
    }

    public void OnCompleted()
    {
        this._inner.OnCompleted();
    }
}