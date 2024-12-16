﻿// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Confluent.SchemaRegistry;

using Epam.Kafka.PubSub.Common.Options;
using Epam.Kafka.PubSub.Publication.Topics;
using Epam.Kafka.PubSub.Replication;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.PubSub.Subscription.State;

using Microsoft.Extensions.Options;

namespace Epam.Kafka.PubSub.Subscription.Options;

/// <summary>
///     Options to configure subscription service.
/// </summary>
public sealed class SubscriptionOptions : PubSubOptions, IOptions<SubscriptionOptions>, IPublicationTopicWrapperOptions
{
    internal Func<Lazy<ISchemaRegistryClient>, object>? KeyDeserializer;

    internal Type StateType = typeof(InternalKafkaState);
    internal Func<Lazy<ISchemaRegistryClient>, object>? ValueDeserializer;

    /// <summary>
    /// 
    /// </summary>
    public ReplicationOptions Replication { get; } = new();

    /// <summary>
    ///     The logical name for <see cref="IConsumer{TKey,TValue}" /> to create it using <see cref="IKafkaFactory" />
    /// </summary>
    public string? Consumer { get; set; }

    /// <summary>
    ///     The timeout between sequential batch executions in case of last result was
    ///     <see cref="SubscriptionBatchResult.Paused" />
    /// </summary>
    public TimeSpan BatchPausedTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     The timeout between sequential batch executions in case of last result was
    ///     <see cref="SubscriptionBatchResult.NotAssigned" />
    /// </summary>
    public TimeSpan BatchNotAssignedTimeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    ///     Whether to additionally commit offsets to kafka internal state if external state provider is used as main offsets
    ///     storage.
    /// </summary>
    public bool ExternalStateCommitToKafka { get; set; }

    /// <summary>
    ///     Semicolon separated list of topic names to subscribe or topic partitions to assign.
    ///     <remarks>
    ///         Topic names format sample: "my-topic1; my-topic2"
    ///         Topic partition format sample: "my-topic1 [0]; my-topic2 [0,1]"
    ///     </remarks>
    /// </summary>
    public string? Topics { get; set; }

    SubscriptionOptions IOptions<SubscriptionOptions>.Value => this;
    object? IPublicationTopicWrapperOptions.CreateKeySerializer(Lazy<ISchemaRegistryClient> lazySchemaRegistryClient)
    {
        return this.Replication.KeySerializer?.Invoke(lazySchemaRegistryClient);
    }

    object? IPublicationTopicWrapperOptions.CreateValueSerializer(Lazy<ISchemaRegistryClient> lazySchemaRegistryClient)
    {
        return this.Replication.ValueSerializer?.Invoke(lazySchemaRegistryClient);
    }

    ProducerPartitioner IPublicationTopicWrapperOptions.GetPartitioner()
    {
        return this.Replication.Partitioner;
    }

    string? IPublicationTopicWrapperOptions.GetDefaultTopic()
    {
        return this.Replication.DefaultTopic;
    }

    string? IPublicationTopicWrapperOptions.GetProducer()
    {
        return this.Replication.Producer;
    }

    bool? IPublicationTopicWrapperOptions.GetSerializationPreprocessor()
    {
        return true;
    }

    string? IPublicationTopicWrapperOptions.GetCluster()
    {
        return this.Replication.Cluster ?? this.Cluster;
    }
}