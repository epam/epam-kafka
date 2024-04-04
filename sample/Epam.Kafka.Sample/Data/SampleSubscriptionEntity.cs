// Copyright © 2024 EPAM Systems

namespace Epam.Kafka.Sample.Data;

public class SampleSubscriptionEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public int Partition { get; set; }

    public long Offset { get; set; }
}