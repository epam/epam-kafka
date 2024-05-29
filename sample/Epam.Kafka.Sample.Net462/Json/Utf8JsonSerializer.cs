// Copyright © 2024 EPAM Systems


using System;
using System.Text.Json;
using Confluent.Kafka;
using Epam.Kafka.Sample.Net462.Data;

namespace Epam.Kafka.Sample.Net462.Json
{
    public class Utf8JsonSerializer : ISerializer<KafkaEntity>, IDeserializer<KafkaEntity>
    {
        private Utf8JsonSerializer()
        {
        }

        public static Utf8JsonSerializer Instance { get; } = new Utf8JsonSerializer();

        public KafkaEntity Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            if (isNull)
            {
                return default;
            }

            return JsonSerializer.Deserialize<KafkaEntity>(data);
        }

        public byte[] Serialize(KafkaEntity data, SerializationContext context)
        {
            return JsonSerializer.SerializeToUtf8Bytes(data);
        }
    }
}