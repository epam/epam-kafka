// Copyright © 2024 EPAM Systems

using Microsoft.Extensions.Options;

namespace Epam.Kafka.Options;

/// <summary>
///     Configuration for default <see cref="IKafkaFactory" /> behaviour.
/// </summary>
public sealed class KafkaFactoryOptions : IOptions<KafkaFactoryOptions>
{
    /// <summary>
    ///     Default cluster config name
    /// </summary>
    public string? Cluster { get; set; }

    /// <summary>
    ///     Default consumer config name
    /// </summary>
    public string? Consumer { get; set; }

    /// <summary>
    ///     Default producer config name
    /// </summary>
    public string? Producer { get; set; }

    KafkaFactoryOptions IOptions<KafkaFactoryOptions>.Value => this;
}