// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

namespace Epam.Kafka.Metrics;

#pragma warning disable CA1001 // dispose only on completed

internal abstract class StatisticsMetrics : IObserver<Statistics>
{
    private const string NameTag = "Name";
    private const string HandlerTag = "Handler";
    private const string InstanceTag = "Instance";

    private bool _initialized;
    private readonly Meter _meter;

    protected Statistics? Value { get; private set; }
    protected static IEnumerable<Measurement<long>> Empty { get; } = Enumerable.Empty<Measurement<long>>();

    protected KeyValuePair<string, object?>[]? TopLevelTags { get; private set; }

    protected StatisticsMetrics(string meterName)
    {
        if (meterName == null) throw new ArgumentNullException(nameof(meterName));

        this._meter = new Meter(meterName);
    }

    public void OnNext(Statistics value)
    {
        this.Value = value;
        this.TopLevelTags ??= new[]
        {
            new KeyValuePair<string, object?>(NameTag, value.ClientId),
            new KeyValuePair<string, object?>(HandlerTag, value.Name),
            new KeyValuePair<string, object?>(InstanceTag, value.Type),
        };

        if (!this._initialized)
        {
            lock (this._meter)
            {
                if (!this._initialized)
                {
                    this.Initialize(this._meter);
                    this._initialized = true;
                }
            }
        }
    }

    protected abstract void Initialize(Meter meter);

    public void OnError(Exception error)
    {
        this.Value = null;
        this.TopLevelTags = null;
    }

    public void OnCompleted()
    {
        this._meter.Dispose();
    }

    protected void CreateTopLevelGauge(Meter meter, string name, Func<Statistics, long> factory)
    {
        if (meter == null) throw new ArgumentNullException(nameof(meter));
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        meter.CreateObservableGauge(name, () =>
        {
            Statistics? value = this.Value;

            if (value is not null)
            {
                return Enumerable.Repeat(
                    new Measurement<long>(factory(value), this.TopLevelTags), 1);
            }

            return Empty;
        });
    }

    protected void CreateTopLevelCounter(Meter meter, string name, Func<Statistics, long> factory, string? unit = null, string? description = null)
    {
        if (meter == null) throw new ArgumentNullException(nameof(meter));
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        meter.CreateObservableCounter(name, () =>
        {
            Statistics? value = this.Value;

            if (value is not null)
            {
                return Enumerable.Repeat(
                    new Measurement<long>(factory(value), this.TopLevelTags), 1);
            }

            return Empty;
        }, unit, description);
    }
}

#pragma warning restore CA1001