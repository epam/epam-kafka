// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

namespace Epam.Kafka.Metrics;

internal sealed class ProducerMetrics : TopLevelMetrics
{
    protected override void Initialize(Meter meter)
    {
        base.Initialize(meter);

        this.CreateTopLevelCounter(meter, "epam_kafka_stats_txmsgs", v => v.TransmittedMessagesTotal,
            description: "Total number of messages transmitted (produced) to Kafka brokers");

        //this.CreateTopLevelCounter(meter, "epam_kafka_stats_tx", v => v.TransmittedRequestsTotal,
        //    description: "Total number of requests sent to Kafka brokers.");
    }
}