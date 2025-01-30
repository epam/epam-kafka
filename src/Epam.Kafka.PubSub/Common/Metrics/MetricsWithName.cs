// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Pipeline;

using System.Diagnostics.Metrics;

namespace Epam.Kafka.PubSub.Common.Metrics;

internal abstract class MetricsWithName : IDisposable
{
    private const string NameTag = "Name";

    private readonly Meter _meter;

    protected MetricsWithName(string name, PipelineMonitor monitor)
    {
        if (name == null) throw new ArgumentNullException(nameof(name));

        this._meter = new Meter(name,null, new[] { new KeyValuePair<string, object?>(NameTag, monitor.FullName) });
    }

    public void Dispose()
    {
        this._meter.Dispose();
    }

    protected void CreateObservableGauge<T>(string name, Func<T> observeValue, string? description) where T : struct
    {
        this._meter.CreateObservableGauge(name, () => new Measurement<T>(observeValue()), null, description);
    }
}