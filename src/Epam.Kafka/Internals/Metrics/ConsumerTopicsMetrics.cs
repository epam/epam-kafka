// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

namespace Epam.Kafka.Internals.Metrics;

internal class ConsumerTopicsMetrics : StatisticsMetrics
{
    public ConsumerTopicsMetrics(Meter meter) : base(meter)
    {
    }

    protected override void Create()
    {
        this.Meter.CreateObservableGauge($"{NamePrefix}_tp_lag",
            () => this.Latest.Topics
                .SelectMany(t => t.Value.Partitions.Where(p => p.Key > -1).Select(p => new { Topic = t.Key, Partition = p }))
                .Select(x => new Measurement<long>(x.Partition.Value.ConsumerLag, this.BuildTpTags(x.Topic, x.Partition.Key))), null,
            "Difference between (hi_offset or ls_offset) and committed_offset). hi_offset is used when isolation.level=read_uncommitted, otherwise ls_offset.");
    }
}