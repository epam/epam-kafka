// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Epam.Kafka.Stats;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web,
    PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
    UseStringEnumConverter = true,
    Converters = new[] { typeof(PartitionConverter) })]
[JsonSerializable(typeof(Statistics))]
internal partial class StatsJsonContext : JsonSerializerContext
{
    public class PartitionConverter : JsonConverter<Partition>
    {

        public override Partition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new Partition(reader.GetInt32());
        }

        public override void Write(Utf8JsonWriter writer, Partition value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Value);
        }
    }
}