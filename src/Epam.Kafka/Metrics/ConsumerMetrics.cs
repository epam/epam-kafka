// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

namespace Epam.Kafka.Metrics;

internal sealed class ConsumerMetrics : TopLevelMetrics
{
    protected override void Initialize(Meter meter)
    {
        base.Initialize(meter);

        this.CreateTopLevelCounter(meter, "epam_kafka_stats_rxmsgs", v => v.ConsumedMessagesTotal,
            description: "Total number of messages consumed, not including ignored messages (due to offset, etc), from Kafka brokers.");

        //this.CreateTopLevelCounter(meter, "epam_kafka_stats_rx", v => v.ConsumedRequestsTotal,
        //    description: "Total number of responses received from Kafka brokers.");
    }
}