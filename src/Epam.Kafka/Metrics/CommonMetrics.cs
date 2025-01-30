// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

namespace Epam.Kafka.Metrics;

internal abstract class CommonMetrics : StatisticsMetrics
{
    protected override void Initialize(Meter meter, Meter topParMeter)
    {
        this.CreateCounter(meter, "epam_kafka_stats_trx_msgs", this.GetTxRxMsg,
            description: "Number of messages consumed or produced.");

        this.CreateCounter(meter, "epam_kafka_stats_trx", this.GetTxRx,
            description: "Number of requests transmitted or received.");

        this.CreateCounter(meter, "epam_kafka_stats_trx_bytes", this.GetTxRxBytes,
            description: "Number of bytes transmitted or received.");

        this.CreateGauge(meter, "epam_kafka_stats_age", v => v.AgeMicroseconds / 1000000, "seconds",
            "Time since this client instance was created (seconds).");

        this.CreateGauge(meter, "epam_kafka_stats_replyq", v => v.OpsQueueCountGauge, description:
            "Number of ops (callbacks, events, etc) waiting in queue for application to serve with rd_kafka_poll().");
    }

    protected abstract long GetTxRxMsg(Statistics value);
    protected abstract long GetTxRx(Statistics value);
    protected abstract long GetTxRxBytes(Statistics value);
}