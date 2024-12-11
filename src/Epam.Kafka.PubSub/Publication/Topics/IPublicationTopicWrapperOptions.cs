// Copyright © 2024 EPAM Systems

using Confluent.SchemaRegistry;

namespace Epam.Kafka.PubSub.Publication.Topics;

internal interface IPublicationTopicWrapperOptions
{
    object? CreateKeySerializer(Lazy<ISchemaRegistryClient> lazySchemaRegistryClient);
    object? CreateValueSerializer(Lazy<ISchemaRegistryClient> lazySchemaRegistryClient);
    ProducerPartitioner GetPartitioner();
    string? DefaultTopic { get; }
    string? Producer { get; }
    string? Cluster { get; }
    bool? SerializationPreprocessor { get; }
    TimeSpan HandlerTimeout { get; }
}