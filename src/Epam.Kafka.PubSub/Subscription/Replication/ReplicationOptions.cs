// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Common.Options;
using Epam.Kafka.PubSub.Publication;

namespace Epam.Kafka.PubSub.Subscription.Replication;

/// <summary>
/// Options to control publication behaviour for subscription that added using <see cref="KafkaBuilderExtensions.AddReplication{TSubKey,TSubValue,TPubKey,TPubValue,THandler}"/>
/// </summary>
public sealed partial class ReplicationOptions
{
    /// <summary>
    ///     Topic name for producer. Mandatory setting.
    ///     <remarks>
    ///         Replication services use this value as default one if message specific value not provided in
    ///         <see cref="TopicMessage{TKey,TValue}.Topic" />
    ///     </remarks>
    /// </summary>
    public string? DefaultTopic { get; set; }

    /// <summary>
    ///     The logical name for <see cref="ProducerConfig"/> that will be used by <see cref="IKafkaFactory" /> to create <see cref="IProducer{TKey,TValue}" />
    /// </summary>
    public string? Producer { get; set; }

    /// <summary>
    /// The logical name for <see cref="ClientConfig"/> that will be used by <see cref="IKafkaFactory" /> to create <see cref="IProducer{TKey,TValue}" />.
    /// If null then value from <see cref="PubSubOptions.Cluster"/> will be used to read and write to same cluster by default.
    /// </summary>
    public string? Cluster { get; set; }
}