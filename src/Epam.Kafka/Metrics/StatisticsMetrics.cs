// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;
using System.Text.RegularExpressions;

namespace Epam.Kafka.Metrics;

#pragma warning disable CA1001 // dispose only on completed

internal abstract class StatisticsMetrics : IObserver<Statistics>
{
    private static readonly Regex HandlerRegex = new("^(.*)#(consumer|producer)-(\\d{1,7})$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private const string NameTag = "Name";
    private const string HandlerTag = "Handler";
    private const string TypeTag = "Type";

    private readonly object _syncObj = new();
    private bool _initialized;
    private readonly List<Meter> _meters = new();

    protected Statistics? Value { get; private set; }
    protected static IEnumerable<Measurement<long>> Empty { get; } = Enumerable.Empty<Measurement<long>>();

    public void OnNext(Statistics value)
    {
        this.Value = value;

        if (!this._initialized)
        {
            lock (this._syncObj)
            {
                if (!this._initialized)
                {
                    Match match = HandlerRegex.Match(value.Name);

                    string name = match.Success ? match.Result("$3") : value.Name;

                    KeyValuePair<string, object?>[] topLevelTags = new[]
                    {
                        new KeyValuePair<string, object?>(NameTag, value.ClientId),
                        new KeyValuePair<string, object?>(HandlerTag, name),
                        new KeyValuePair<string, object?>(TypeTag, value.Type),
                    };

                    this._initialized = true;

                    foreach (Meter meter in this.Initialize(topLevelTags))
                    {
                        this._meters.Add(meter);
                    }
                }
            }
        }
    }

    protected abstract IEnumerable<Meter> Initialize(KeyValuePair<string, object?>[] topLevelTags);

    public void OnError(Exception error)
    {
        this.Value = null;
    }

    public void OnCompleted()
    {
        foreach (Meter meter in this._meters)
        {
#pragma warning disable CA1031 // don't prevent other meters dispose
            try
            {
                meter.Dispose();
            }
            catch
            {
            }
#pragma warning restore CA1031
        }
    }

    protected void CreateGauge(Meter meter, string name, Func<Statistics, long> factory, string? unit = null, string? description = null)
    {
        if (meter == null) throw new ArgumentNullException(nameof(meter));
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        meter.CreateObservableGauge(name, () =>
        {
            Statistics? value = this.Value;

            if (value is not null)
            {
                return Enumerable.Repeat(new Measurement<long>(factory(value)), 1);
            }

            return Empty;
        }, unit, description);
    }

    protected void CreateCounter(Meter meter, string name, Func<Statistics, long> factory, string? unit = null, string? description = null)
    {
        if (meter == null) throw new ArgumentNullException(nameof(meter));
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        meter.CreateObservableCounter(name, () =>
        {
            Statistics? value = this.Value;

            if (value is not null)
            {
                return Enumerable.Repeat(new Measurement<long>(factory(value)), 1);
            }

            return Empty;
        }, unit, description);
    }
}

#pragma warning restore CA1001