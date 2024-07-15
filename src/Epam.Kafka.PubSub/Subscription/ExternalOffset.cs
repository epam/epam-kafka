// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

namespace Epam.Kafka.PubSub.Subscription;

/// <summary>
/// Holds additional special offsets supported by <see cref="IExternalOffsetsStorage"/> only.
/// </summary>
public static class ExternalOffset
{
    /// <summary>
    /// Indicates that subscription should be paused
    /// </summary>
    public static Offset Paused { get; } = -863;
}