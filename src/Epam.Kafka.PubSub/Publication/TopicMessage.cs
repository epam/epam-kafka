// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Common.Options;

namespace Epam.Kafka.PubSub.Publication;

/// <summary>
///     Represent <see cref="Message{TKey,TValue}" /> with target topic.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class TopicMessage<TKey, TValue> : Message<TKey, TValue>
{
    /// <summary>
    ///     Target topic value or null to publish to default topic specified in <see cref="PubSubOptions" />.
    /// </summary>
    public string? Topic { get; set; }
}