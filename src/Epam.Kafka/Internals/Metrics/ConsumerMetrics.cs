// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

namespace Epam.Kafka.Internals.Metrics;

internal sealed class ConsumerMetrics : StatisticsMetrics
{
    private readonly Meter _meter;

    public ConsumerMetrics(Meter meter)
    {
        this._meter = meter ?? throw new ArgumentNullException(nameof(meter));
    }

    protected override void Create()
    {
        this._meter.CreateObservableCounter($"{NamePrefix}_rxmsgs",
            () => new Measurement<long>(this.Latest.ConsumedMessagesTotal, this.Tags), null,
            "Total number of messages consumed, not including ignored messages (due to offset, etc).");
    }
}