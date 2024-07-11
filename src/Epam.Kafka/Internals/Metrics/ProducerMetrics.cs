// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

namespace Epam.Kafka.Internals.Metrics;

internal sealed class ProducerMetrics : StatisticsMetrics
{
    public ProducerMetrics(Meter meter) : base(meter)
    {
    }

    protected override void Create()
    {
        this.Meter.CreateObservableCounter($"{NamePrefix}_txmsgs",
            () => new Measurement<long>(this.Latest.TransmittedMessagesTotal, this.Tags), null,
            "Total number of messages transmitted (produced).");
    }
}