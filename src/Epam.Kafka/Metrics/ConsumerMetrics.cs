// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

using Confluent.Kafka;

using Epam.Kafka.Stats.Topic;

namespace Epam.Kafka.Metrics;

internal sealed class ConsumerMetrics : CommonMetrics
{
    private const string DesiredTagName = "Desired";
    private const string TopicTagName = "Topic";
    private const string PartitionTagName = "Partition";
    private const string ConsumerGroupTagName = "Group";

    private readonly ConsumerConfig _config;

    public ConsumerMetrics(ConsumerConfig config)
    {
        this._config = config ?? throw new ArgumentNullException(nameof(config));
    }

    protected override void Initialize(Func<string, IEnumerable<KeyValuePair<string, object?>>?, Meter> meterFactory)
    {
        base.Initialize(meterFactory);

        KeyValuePair<string, object?>[] groupTag = new[]
        {
            new KeyValuePair<string, object?>(ConsumerGroupTagName, this._config.GroupId)
        };

        Meter cgMeter = meterFactory(Statistics.ConsumerGroupMeterName, null);

        this.ConfigureCgMeter(cgMeter);

        Meter topParMeter = meterFactory(Statistics.TopicPartitionMeterName, groupTag);

        this.ConfigureTopParMeter(topParMeter);
    }

    private void ConfigureCgMeter(Meter cgMeter)
    {
        cgMeter.CreateObservableGauge("epam_kafka_stats_cg_state", () => (long)this.Value!.ConsumerGroup.State,
            null, "Consumer group handler state");

        cgMeter.CreateObservableGauge("epam_kafka_stats_cg_join_state", () => (long)this.Value!.ConsumerGroup.JoinState,
            null, "Consumer group handler join state");

        cgMeter.CreateObservableGauge("epam_kafka_stats_cg_rebalance_age", () => this.Value!.ConsumerGroup.RebalanceAgeMilliseconds / 1000,
            "seconds", "Time elapsed since last rebalance seconds");

        cgMeter.CreateObservableCounter("epam_kafka_stats_cg_rebalance_count", () => this.Value!.ConsumerGroup.RebalanceCount,
            null, "Total number of rebalances");

        cgMeter.CreateObservableGauge("epam_kafka_stats_cg_assignment_count", () => this.Value!.ConsumerGroup.AssignmentCount,
            null, "Current assignment's partition count");
    }

    private void ConfigureTopParMeter(Meter topParMeter)
    {
        topParMeter.CreateObservableGauge("epam_kafka_stats_tp_fetch_state", () => this.Value!.Topics
                .SelectMany(p =>
                    p.Value.Partitions.Where(x => x.Key != PartitionStatistics.InternalUnassignedPartition)
                        .Select(x => new KeyValuePair<TopicStatistics, PartitionStatistics>(p.Value, x.Value)))
                .Select(m => new Measurement<long>((long)m.Value.FetchState, new[]
                {
                    new KeyValuePair<string, object?>(DesiredTagName, m.Value.Desired),
                    new KeyValuePair<string, object?>(TopicTagName, m.Key.Name),
                    new KeyValuePair<string, object?>(PartitionTagName, m.Value.Id),
                })),
            null, "Consumer lag");

        topParMeter.CreateObservableGauge("epam_kafka_stats_tp_lag", () => this.Value!.Topics
                .SelectMany(p =>
                    p.Value.Partitions.Where(x => x.Key != PartitionStatistics.InternalUnassignedPartition)
                        .Select(x => new KeyValuePair<TopicStatistics, PartitionStatistics>(p.Value, x.Value)))
                .Select(m => new Measurement<long>(m.Value.ConsumerLag, new[]
                {
                    new KeyValuePair<string, object?>(DesiredTagName, m.Value.Desired),
                    new KeyValuePair<string, object?>(TopicTagName, m.Key.Name),
                    new KeyValuePair<string, object?>(PartitionTagName, m.Value.Id),
                })),
            null, "Consumer lag");
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