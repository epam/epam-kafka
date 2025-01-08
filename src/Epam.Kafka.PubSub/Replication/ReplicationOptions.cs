// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Publication;

namespace Epam.Kafka.PubSub.Replication;

/// <summary>
/// 
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
    ///     The logical name for <see cref="IProducer{TKey,TValue}" /> to create it using <see cref="IKafkaFactory" />
    /// </summary>
    public string? Producer { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? Cluster { get; set; }
}