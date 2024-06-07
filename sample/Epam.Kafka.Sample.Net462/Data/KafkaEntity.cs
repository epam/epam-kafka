// Copyright © 2024 EPAM Systems

using System;

namespace Epam.Kafka.Sample.Net462.Data
{
    public class KafkaEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
    }
}