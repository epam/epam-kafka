// Copyright © 2024 EPAM Systems

using Epam.Kafka.Sample.Data;

using System.Text.Json.Serialization;

namespace Epam.Kafka.Sample.Json;

[JsonSerializable(typeof(KafkaEntity))]
public partial class JsonContext : JsonSerializerContext
{
}