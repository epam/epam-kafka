﻿// Copyright © 2024 EPAM Systems

using Confluent.SchemaRegistry;

namespace Epam.Kafka.PubSub.Publication.Topics;

internal interface IPublicationTopicWrapperOptions
{
    object? CreateKeySerializer(Lazy<ISchemaRegistryClient> lazySchemaRegistryClient);
    object? CreateValueSerializer(Lazy<ISchemaRegistryClient> lazySchemaRegistryClient);
    ProducerPartitioner GetPartitioner();
    string? GetDefaultTopic();
    string? GetProducer();
    string? GetCluster();
    bool? GetSerializationPreprocessor();
}