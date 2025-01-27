// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

namespace Epam.Kafka.Metrics;

internal sealed class ProducerMetrics : StatisticsMetrics
{
    public ProducerMetrics() : base(Statistics.MeterName)
    {

    }

    protected override void Initialize(Meter meter)
    {
        this.CreateTopLevelCounter(meter, "epam_kafka_stats_txmsgs", v => v.TransmittedMessagesTotal);
        this.CreateTopLevelCounter(meter, "epam_kafka_stats_age", v => v.AgeMicroseconds);
    }
}