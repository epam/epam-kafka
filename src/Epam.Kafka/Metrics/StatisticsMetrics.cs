﻿// Copyright © 2024 EPAM Systems

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
    private KeyValuePair<string, object?>[] _topLevelTags = null!;

    protected Statistics? Value { get; private set; }

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

                    this._topLevelTags = new[]
                    {
                        new KeyValuePair<string, object?>(NameTag, value.ClientId),
                        new KeyValuePair<string, object?>(HandlerTag, name),
                        new KeyValuePair<string, object?>(TypeTag, value.Type),
                    };

                    this._initialized = true;

                    this.Initialize(this.CreateMeter);
                }
            }
        }
    }

    protected abstract void Initialize(Func<string, IEnumerable<KeyValuePair<string, object?>>?, Meter> meterFactory);

    private Meter CreateMeter(string name, IEnumerable<KeyValuePair<string, object?>>? tags = null)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));

        IEnumerable<KeyValuePair<string, object?>> resultTags = this._topLevelTags;

        if (tags != null)
        {
            resultTags = resultTags.Concat(tags);
        }

        Meter meter = new(name, null, resultTags);

        this._meters.Add(meter);

        return meter;
    }

    public void OnError(Exception error)
    {
        this.OnCompleted();
        this._initialized = false;
    }

    public void OnCompleted()
    {
        foreach (Meter meter in this._meters)
        {
            meter.Dispose();
        }

        this._meters.Clear();
        this.Value = null;
    }
}

#pragma warning restore CA1001