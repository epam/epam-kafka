// Copyright © 2024 EPAM Systems

using System.Text.Json.Serialization;

namespace Epam.Kafka.Stats;

/// <summary>
/// Enum representing the states of consumer fetch for a partition.
/// </summary>
public enum PartitionFetchState
{
    /// <summary>
    /// No fetch activity.
    /// </summary>
    [JsonStringEnumMemberName("none")]
    None,

    /// <summary>
    /// Fetching is stopping.
    /// </summary>
    [JsonStringEnumMemberName("stopping")]
    Stopping,

    /// <summary>
    /// Fetching has been stopped.
    /// </summary>
    [JsonStringEnumMemberName("stopped")]
    Stopped,

    /// <summary>
    /// Querying for offsets.
    /// </summary>
    [JsonStringEnumMemberName("offset-query")]
    OffsetQuery,

    /// <summary>
    /// Waiting for offset confirmation.
    /// </summary>
    [JsonStringEnumMemberName("offset-wait")]
    OffsetWait,

    /// <summary>
    /// Actively fetching data.
    /// </summary>
    [JsonStringEnumMemberName("active")]
    Active
}