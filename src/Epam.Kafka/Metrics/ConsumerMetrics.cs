// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

using Confluent.Kafka;

using Epam.Kafka.Stats;

namespace Epam.Kafka.Metrics;

internal sealed class ConsumerMetrics : CommonMetrics
{
    private const string StateTagName = "State";
    private const string ReasonTagName = "Reason";
    private const string TopicTagName = "Topic";
    private const string PartitionTagName = "Partition";
    private const string ConsumerGroupTagName = "Group";

    private readonly ConsumerConfig _config;

    public ConsumerMetrics(ConsumerConfig config)
    {
        this._config = config ?? throw new ArgumentNullException(nameof(config));
    }

    protected override IEnumerable<Meter> Initialize(KeyValuePair<string, object?>[] topLevelTags)
    {
        foreach (Meter m in base.Initialize(topLevelTags))
        {
            yield return m;
        }

        KeyValuePair<string, object?>[] tags = topLevelTags.Concat(new[]
        {
            new KeyValuePair<string, object?>(ConsumerGroupTagName, this._config.GroupId)
        }).ToArray();

        Meter cgMeter = new(Statistics.ConsumerGroupMeterName, null, tags);

        this.ConfigureCgMeter(cgMeter);

        yield return cgMeter;

        Meter topParMeter = new(Statistics.TopicPartitionMeterName, null, tags);

        this.ConfigureTopParMeter(topParMeter);

        yield return topParMeter;
    }

    private void ConfigureCgMeter(Meter cgMeter)
    {
        cgMeter.CreateObservableGauge("epam_kafka_stats_cg_state_age", () =>
        {
            Statistics? v = this.Value;

            if (v != null)
            {
                return Enumerable.Repeat(new Measurement<long>(v.ConsumerGroup.StateAgeMilliseconds / 1000,
                    new[]
                    {
                        new KeyValuePair<string, object?>(StateTagName, v.ConsumerGroup.State)
                    }), 1);
            }

            return Empty;
        }, "seconds", "Consumer group handler state age seconds");

        cgMeter.CreateObservableGauge("epam_kafka_stats_cg_rebalance_age", () =>
        {
            Statistics? v = this.Value;

            if (v != null)
            {
                return Enumerable.Repeat(new Measurement<long>(v.ConsumerGroup.RebalanceAgeMilliseconds / 1000,
                    new[]
                    {
                        new KeyValuePair<string, object?>(ReasonTagName, v.ConsumerGroup.RebalanceReason)
                    }), 1);
            }

            return Empty;
        }, "seconds", "Time elapsed since last rebalance seconds");

        cgMeter.CreateObservableCounter("epam_kafka_stats_cg_rebalance_count", () =>
        {
            Statistics? v = this.Value;

            if (v != null)
            {
                return Enumerable.Repeat(new Measurement<long>(v.ConsumerGroup.RebalanceCount), 1);
            }

            return Empty;
        }, null, "Total number of rebalances");

        cgMeter.CreateObservableGauge("epam_kafka_stats_cg_assignment_count", () =>
        {
            Statistics? v = this.Value;

            if (v != null)
            {
                return Enumerable.Repeat(new Measurement<long>(v.ConsumerGroup.AssignmentCount,
                    new[]
                    {
                        new KeyValuePair<string, object?>(StateTagName, v.ConsumerGroup.JoinState)
                    }), 1);
            }

            return Empty;
        }, null, "Current assignment's partition count");
    }

    private void ConfigureTopParMeter(Meter topParMeter)
    {
        topParMeter.CreateObservableGauge("epam_kafka_stats_tp_lag", () =>
        {
            Statistics? v = this.Value;

            if (v != null)
            {
                return v.Topics
                    .SelectMany(p =>
                        p.Value.Partitions.Where(x =>
                                x.Key != PartitionStatistics.InternalUnassignedPartition &&
                                x.Value is { Desired: true, ConsumerLag: >= 0 })
                            .Select(x => new KeyValuePair<TopicStatistics, PartitionStatistics>(p.Value, x.Value)))
                    .Select(m => new Measurement<long>(m.Value.ConsumerLag, new[]
                    {
                        new KeyValuePair<string, object?>(TopicTagName, m.Key.Name),
                        new KeyValuePair<string, object?>(PartitionTagName, m.Value.Id),
                    }));
            }

            return Empty;
        }, null, "Consumer lag");
    }

    protected override long GetTxRxMsg(Statistics value)
    {
        return value.ConsumedMessagesTotal;
    }

    protected override long GetTxRx(Statistics value)
    {
        return value.ConsumedRequestsTotal;
    }

    protected override long GetTxRxBytes(Statistics value)
    {
        return value.ConsumedBytesTotal;
    }
}