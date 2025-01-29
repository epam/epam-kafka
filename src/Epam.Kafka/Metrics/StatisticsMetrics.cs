﻿// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;
using Epam.Kafka.Stats;

namespace Epam.Kafka.Metrics;

#pragma warning disable CA1001 // dispose only on completed

internal abstract class StatisticsMetrics : IObserver<Statistics>
{
    private const string NameTag = "Name";
    private const string HandlerTag = "Handler";
    private const string InstanceTag = "Instance";
    private const string TopicTagName = "Topic";
    private const string PartitionTagName = "Partition";

    private readonly object _syncObj = new();
    private bool _initialized;
    private Meter? _topLevelMeter;
    private Meter? _topParMeter;

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
                    KeyValuePair<string, object?>[] topLevelTags = new[]
                    {
                        new KeyValuePair<string, object?>(NameTag, value.ClientId),
                        new KeyValuePair<string, object?>(HandlerTag, value.Name),
                        new KeyValuePair<string, object?>(InstanceTag, value.Type),
                    };

                    this._topLevelMeter = new Meter(Statistics.TopLevelMeterName, null, topLevelTags);
                    this._topParMeter = new Meter(Statistics.TopicPartitionMeterName, null, topLevelTags);

                    this.Initialize(this._topLevelMeter, this._topParMeter);

                    this._initialized = true;
                }
            }
        }
    }

    protected abstract void Initialize(Meter meter, Meter topParMeter);

    public void OnError(Exception error)
    {
        this.Value = null;
    }

    public void OnCompleted()
    {
        this._topLevelMeter?.Dispose();
        this._topParMeter?.Dispose();
    }

    protected void CreateGauge(Meter meter, string name, Func<Statistics, long> factory)
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
        });
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

    protected void CreateTpGauge(Meter meter, string name, Func<KeyValuePair<TopicStatistics,PartitionStatistics>,long> factory, string? unit = null,
        string? description = null)
    {
        if (meter == null) throw new ArgumentNullException(nameof(meter));
        if (name == null) throw new ArgumentNullException(nameof(name));

        meter.CreateObservableGauge(name, () =>
        {
            Statistics? v = this.Value;

            if (v != null)
            {
                return v.Topics
                    .SelectMany(p =>
                        p.Value.Partitions.Where(x => x.Key != PartitionStatistics.InternalUnassignedPartition)
                            .Select(x => new KeyValuePair<TopicStatistics, PartitionStatistics>(p.Value, x.Value)))
                    .Select(m => new Measurement<long>(factory(m), new[]
                    {
                        new KeyValuePair<string, object?>(TopicTagName, m.Key.Name),
                        new KeyValuePair<string, object?>(PartitionTagName, m.Value.Id)
                    }));
            }

            return Empty;
        }, unit, description);
    }
}

#pragma warning restore CA1001