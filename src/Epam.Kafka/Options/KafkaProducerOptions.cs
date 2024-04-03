// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Microsoft.Extensions.Options;

namespace Epam.Kafka.Options;

/// <summary>
///     Options for KAFKA producer config.
/// </summary>
public sealed class KafkaProducerOptions : IOptions<KafkaProducerOptions>
{
    /// <summary>
    ///     The <see cref="Confluent.Kafka.ProducerConfig" />.
    /// </summary>
    public ProducerConfig ProducerConfig { get; set; } = new();

    KafkaProducerOptions IOptions<KafkaProducerOptions>.Value => this;
}