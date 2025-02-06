// Copyright © 2024 EPAM Systems

using System.Text.Json.Serialization;

namespace Epam.Kafka.Stats.Group;

/// <summary>
/// Enum representing the local consumer group handler's overall states in librdkafka.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<GroupState>))]
public enum GroupState
{
    /// <summary>
    /// Not available
    /// </summary>
    None,

    /// <summary>
    /// Initial state before joining the consumer group.
    /// </summary>
    [JsonStringEnumMemberName("init")]
    Init,

    /// <summary>
    /// Waiting for the broker to respond during the join process.
    /// </summary>
    [JsonStringEnumMemberName("wait-broker")]
    WaitBroker,

    /// <summary>
    /// Waiting for the consumer group to get synchronized.
    /// </summary>
    [JsonStringEnumMemberName("wait-sync")]
    WaitSync,

    /// <summary>
    /// Successfully joined and synchronized with the consumer group.
    /// </summary>
    [JsonStringEnumMemberName("up")]
    Up,

    /// <summary>
    /// Failure during the join process or disconnected from the group.
    /// </summary>
    [JsonStringEnumMemberName("down")]
    Down,

    /// <summary>
    /// Rebalancing consumer partitions within the group.
    /// </summary>
    [JsonStringEnumMemberName("rebalance")]
    Rebalance,

    /// <summary>
    /// Exiting from the consumer group.
    /// </summary>
    [JsonStringEnumMemberName("exit")]
    Exit
}