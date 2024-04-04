// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.Sample.Data;

using System.Text.Json;

namespace Epam.Kafka.Sample.Json;

public class Utf8JsonSerializer : ISerializer<KafkaEntity>, IDeserializer<KafkaEntity?>
{
    private Utf8JsonSerializer()
    {
    }

    public static Utf8JsonSerializer Instance { get; } = new();

    public KafkaEntity? Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull)
        {
            return default;
        }

        return JsonSerializer.Deserialize(data, JsonContext.Default.KafkaEntity);
    }

    public byte[] Serialize(KafkaEntity data, SerializationContext context)
    {
        return JsonSerializer.SerializeToUtf8Bytes(data, JsonContext.Default.KafkaEntity);
    }
}