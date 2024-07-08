// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

namespace Epam.Kafka.Internals.Metrics;

internal sealed class ConsumerMetrics : StatisticsMetrics
{
    public ConsumerMetrics(Meter meter) : base(meter)
    {
    }

    protected override void Create()
    {
        this.Meter.CreateObservableCounter($"{NamePrefix}_rxmsgs",
            () => new Measurement<long>(this.Latest.ConsumedMessagesTotal, this.Tags), null,
            "Total number of messages consumed, not including ignored messages (due to offset, etc).");

        this.Meter.CreateObservableGauge($"{NamePrefix}_cgrp_state", this.ObserveCgrpState, null,
            "Epoch time in seconds when local consumer group handler's state was observed.");

        this.Meter.CreateObservableGauge($"{NamePrefix}_cgrp_join_state", this.ObserveCgrpJoinState, null,
            "Epoch time in seconds when local consumer group handler's join state was observed.");

        this.Meter.CreateObservableCounter($"{NamePrefix}_cgrp_rebalance_count", this.ObserveCgrpRebalanceCount, null,
            "Total number of re-balances (assign or revoke).");
    }

    private IEnumerable<Measurement<long>> ObserveCgrpState()
    {
        if (this.Latest.ConsumerGroups != null)
        {
            yield return this.CreateStatusMetric(this.Latest.ConsumerGroups.State);
        }
    }

    private IEnumerable<Measurement<long>> ObserveCgrpJoinState()
    {
        if (this.Latest.ConsumerGroups != null)
        {
            yield return this.CreateStatusMetric(this.Latest.ConsumerGroups.JoinState);
        }
    }

    private IEnumerable<Measurement<long>> ObserveCgrpRebalanceCount()
    {
        if (this.Latest.ConsumerGroups != null)
        {
            yield return new Measurement<long>(this.Latest.ConsumerGroups.RebalanceCount, this.Tags);
        }
    }
}