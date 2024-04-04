// Copyright © 2024 EPAM Systems

namespace Epam.Kafka.Sample.Data;

public class KafkaEntity
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
}