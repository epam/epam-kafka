// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

namespace Epam.Kafka.Metrics;

internal class TopLevelMetrics : StatisticsMetrics
{
    protected TopLevelMetrics() : base(Statistics.MeterName)
    {
    }

    protected override void Initialize(Meter meter)
    {
        this.CreateTopLevelCounter(meter, "epam_kafka_stats_age", v => v.AgeMicroseconds, "microseconds",
            "Time since this client instance was created (microseconds).");

        //this.CreateTopLevelCounter(meter, "epam_kafka_stats_replyq", v => v.OpsQueueCountGauge, description:
        //    "Number of ops (callbacks, events, etc) waiting in queue for application to serve with rd_kafka_poll().");
    }
}