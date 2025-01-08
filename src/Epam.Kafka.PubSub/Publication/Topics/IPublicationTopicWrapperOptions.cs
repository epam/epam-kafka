// Copyright © 2024 EPAM Systems

using Confluent.SchemaRegistry;

namespace Epam.Kafka.PubSub.Publication.Topics;

internal interface IPublicationTopicWrapperOptions
{
    Type? GetKeyType();
    Type? GetValueType();

    Func<Lazy<ISchemaRegistryClient>, object>? GetKeySerializer();
    Func<Lazy<ISchemaRegistryClient>, object>? GetValueSerializer();

    // can't be public property due to configuration source generation
    ProducerPartitioner GetPartitioner();
    string? GetDefaultTopic();
    string? GetProducer();
    string? GetCluster();
    bool? GetSerializationPreprocessor();
}