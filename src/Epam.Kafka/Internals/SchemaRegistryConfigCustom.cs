// Copyright © 2024 EPAM Systems

using Confluent.SchemaRegistry;

namespace Epam.Kafka.Internals;

internal class SchemaRegistryConfigCustom : SchemaRegistryConfig
{
    public SchemaRegistryConfigCustom(Dictionary<string, string> values)
    {
        this.properties = values ?? throw new ArgumentNullException(nameof(values));
    }
}