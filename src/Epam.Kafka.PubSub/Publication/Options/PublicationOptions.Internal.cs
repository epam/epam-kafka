// Copyright © 2024 EPAM Systems

using Confluent.SchemaRegistry;

using Epam.Kafka.PubSub.Publication.Topics;

namespace Epam.Kafka.PubSub.Publication.Options;

/// <summary>
///     Options to configure publication service.
/// </summary>
public sealed partial class PublicationOptions : IPublicationTopicWrapperOptions
{
    // can't be public property due to configuration source generation
    internal readonly ProducerPartitioner Partitioner = new();
    
    internal Func<Lazy<ISchemaRegistryClient>, object>? KeySerializer;

    internal Func<Lazy<ISchemaRegistryClient>, object>? ValueSerializer;

    Type? IPublicationTopicWrapperOptions.GetKeyType()
    {
        return this.KeyType;
    }

    Type? IPublicationTopicWrapperOptions.GetValueType()
    {
        return this.ValueType;
    }

    Func<Lazy<ISchemaRegistryClient>, object>? IPublicationTopicWrapperOptions.GetKeySerializer()
    {
        return this.KeySerializer;
    }

    Func<Lazy<ISchemaRegistryClient>, object>? IPublicationTopicWrapperOptions.GetValueSerializer()
    {
        return this.ValueSerializer;
    }

    ProducerPartitioner IPublicationTopicWrapperOptions.GetPartitioner()
    {
        return this.Partitioner;
    }

    string? IPublicationTopicWrapperOptions.GetDefaultTopic()
    {
        return this.DefaultTopic;
    }

    string? IPublicationTopicWrapperOptions.GetProducer()
    {
        return this.Producer;
    }

    string? IPublicationTopicWrapperOptions.GetCluster()
    {
        return this.Cluster;
    }

    bool? IPublicationTopicWrapperOptions.GetSerializationPreprocessor()
    {
        return this.SerializationPreprocessor;
    }
}