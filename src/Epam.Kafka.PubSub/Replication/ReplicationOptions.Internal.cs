// Copyright © 2024 EPAM Systems

using Confluent.SchemaRegistry;

using Epam.Kafka.PubSub.Publication.Topics;

namespace Epam.Kafka.PubSub.Replication;

/// <summary>
/// 
/// </summary>
public sealed partial class ReplicationOptions : IPublicationTopicWrapperOptions
{
    internal Type? ConvertHandlerType;
    internal Type? KeyType;
    internal Type? ValueType;

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

    bool? IPublicationTopicWrapperOptions.GetSerializationPreprocessor()
    {
        return true;
    }

    string? IPublicationTopicWrapperOptions.GetCluster()
    {
        return this.Cluster;
    }
}