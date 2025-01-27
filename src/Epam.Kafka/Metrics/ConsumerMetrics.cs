// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

namespace Epam.Kafka.Metrics;

internal sealed class ConsumerMetrics : StatisticsMetrics
{
    public ConsumerMetrics() : base(Statistics.MeterName)
    {
    }

    protected override void Initialize(Meter meter)
    {
        this.CreateTopLevelCounter(meter, "epam_kafka_stats_rxmsgs", v => v.ConsumedMessagesTotal);
        this.CreateTopLevelCounter(meter, "epam_kafka_stats_age", v => v.AgeMicroseconds);
    }
}