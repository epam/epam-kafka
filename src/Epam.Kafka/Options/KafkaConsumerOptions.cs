// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Microsoft.Extensions.Options;

namespace Epam.Kafka.Options;

/// <summary>
///     Options for KAFKA consumer config.
/// </summary>
public sealed class KafkaConsumerOptions : IOptions<KafkaConsumerOptions>
{
    /// <summary>
    ///     The <see cref="Confluent.Kafka.ConsumerConfig" />.
    /// </summary>
    public ConsumerConfig ConsumerConfig { get; set; } = new();

    KafkaConsumerOptions IOptions<KafkaConsumerOptions>.Value => this;
}