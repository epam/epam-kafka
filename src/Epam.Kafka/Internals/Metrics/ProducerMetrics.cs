// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

namespace Epam.Kafka.Internals.Metrics;

internal sealed class ProducerMetrics : StatisticsMetrics
{
    private readonly Meter _meter;

    public ProducerMetrics(Meter meter)
    {
        this._meter = meter ?? throw new ArgumentNullException(nameof(meter));
    }

    protected override void Create()
    {
        this._meter.CreateObservableCounter($"{NamePrefix}_txmsgs",
            () => new Measurement<long>(this.Latest.TransmittedMessagesTotal, this.Tags), null,
            "Total number of messages transmitted (produced).");
    }
}