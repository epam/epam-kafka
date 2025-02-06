// Copyright © 2024 EPAM Systems

using System.Text.Json.Serialization;

namespace Epam.Kafka.Stats.Group;

/// <summary>
/// Enum representing the local consumer group handler's join states in librdkafka.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<GroupJoinState>))]
public enum GroupJoinState
{
    /// <summary>
    /// Not available
    /// </summary>
    None,

    /// <summary>
    /// Initial join state, upon starting or after leaving a group.
    /// </summary>
    [JsonStringEnumMemberName("init")]
    Init,

    /// <summary>
    /// Awaiting JoinGroup response from the broker.
    /// </summary>
    [JsonStringEnumMemberName("wait-join")]
    WaitJoin,

    /// <summary>
    /// Waiting for metadata refresh (after joining the group).
    /// </summary>
    [JsonStringEnumMemberName("wait-metadata")]
    WaitMetadata,

    /// <summary>
    /// Awaiting SyncGroup response from the broker.
    /// </summary>
    [JsonStringEnumMemberName("wait-sync")]
    WaitSync,

    /// <summary>
    /// Waiting for revocation callback to be called by the application when rebalancing.
    /// </summary>
    [JsonStringEnumMemberName("wait-revoke")]
    WaitRevoke,

    /// <summary>
    /// Waiting for unassignment of partitions to finish.
    /// </summary>
    [JsonStringEnumMemberName("wait-unassign")]
    WaitUnassign,

    /// <summary>
    /// Waiting for assignment of partitions to complete.
    /// </summary>
    [JsonStringEnumMemberName("wait-assign")]
    WaitAssign,

    /// <summary>
    /// Joined and synced with the group; in steady state.
    /// </summary>
    [JsonStringEnumMemberName("steady")]
    Steady,

    /// <summary>
    /// In the process of leaving the group.
    /// </summary>
    [JsonStringEnumMemberName("leaving")]
    Leaving
}