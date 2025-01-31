// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

using Confluent.Kafka;

using Epam.Kafka.Stats;

namespace Epam.Kafka.Metrics;

internal sealed class ConsumerMetrics : CommonMetrics
{
    private const string DesiredTagName = "Desired";
    private const string FetchTagName = "Fetch";
    private const string TopicTagName = "Topic";
    private const string PartitionTagName = "Partition";
    private const string ConsumerGroupTagName = "Group";

    private readonly ConsumerConfig _config;

    public ConsumerMetrics(ConsumerConfig config)
    {
        this._config = config ?? throw new ArgumentNullException(nameof(config));
    }

    protected override void Initialize(Meter meter, Meter topParMeter)
    {
        base.Initialize(meter, topParMeter);

        this.CreateTpGauge(topParMeter, "epam_kafka_stats_tp_lag",
            m => m.Value.ConsumerLag, null, "Consumer lag");
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

    private void CreateTpGauge(Meter meter, string name,
        Func<KeyValuePair<TopicStatistics, PartitionStatistics>, long> factory,
        string? unit = null,
        string? description = null)
    {
        if (meter == null) throw new ArgumentNullException(nameof(meter));
        if (name == null) throw new ArgumentNullException(nameof(name));

        meter.CreateObservableGauge(name, () =>
        {
            Statistics? v = this.Value;

            if (v != null)
            {
                return v.Topics
                    .SelectMany(p =>
                        p.Value.Partitions.Where(x => x.Key != PartitionStatistics.InternalUnassignedPartition)
                            .Select(x => new KeyValuePair<TopicStatistics, PartitionStatistics>(p.Value, x.Value)))
                    .Select(m => new Measurement<long>(factory(m), new[]
                    {
                        new KeyValuePair<string, object?>(DesiredTagName, m.Value.Desired),
                        new KeyValuePair<string, object?>(FetchTagName, m.Value.FetchState),
                        new KeyValuePair<string, object?>(TopicTagName, m.Key.Name),
                        new KeyValuePair<string, object?>(PartitionTagName, m.Value.Id),
                        new KeyValuePair<string, object?>(ConsumerGroupTagName, this._config.GroupId)
                    }));
            }

            return Empty;
        }, unit, description);
    }
}