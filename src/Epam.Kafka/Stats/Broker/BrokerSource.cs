// Copyright © 2024 EPAM Systems

using System.Text.Json.Serialization;

namespace Epam.Kafka.Stats.Broker;

/// <summary>
/// Enum representing the source state of a Broker in librdkafka.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BrokerSource>))]
public enum BrokerSource
{
    /// <summary>
    /// Not available
    /// </summary>
    None,

    /// <summary>
    /// State indicating the broker has been learned through broker metadata.
    /// </summary>
    [JsonStringEnumMemberName("learned")]
    Learned,

    /// <summary>
    /// State indicating the broker has been configured by the user.
    /// </summary>
    [JsonStringEnumMemberName("configured")]
    Configured,

    /// <summary>
    /// State indicating the broker is managed internally by the system.
    /// </summary>
    [JsonStringEnumMemberName("internal")]
    Internal,

    /// <summary>
    /// State indicating the broker acts as a logical broker within the Kafka cluster.
    /// </summary>
    [JsonStringEnumMemberName("logical")]
    Logical
}