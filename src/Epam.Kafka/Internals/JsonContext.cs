// Copyright © 2024 EPAM Systems

using System.Text.Json.Serialization;

namespace Epam.Kafka.Internals;

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(Statistics))]
internal partial class JsonContext : JsonSerializerContext
{

}