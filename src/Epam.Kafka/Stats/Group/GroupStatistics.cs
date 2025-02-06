// Copyright © 2024 EPAM Systems

using System.Text.Json.Serialization;

namespace Epam.Kafka.Stats.Group;

/// <summary>
/// Consumer group metrics.
/// </summary>
public class GroupStatistics
{
    /// <summary>
    /// Local consumer group handler's state.
    /// </summary>
    [JsonPropertyName("state")]
    public GroupState State { get; set; } = GroupState.None;

    /// <summary>
    /// Time elapsed since last state change (milliseconds).
    /// </summary>
    [JsonPropertyName("stateage")]
    public long StateAgeMilliseconds { get; set; }

    /// <summary>
    /// Current assignment's partition count.
    /// </summary>
    [JsonPropertyName("assignment_size")]
    public long AssignmentCount { get; set; }

    /// <summary>
    /// Local consumer group handler's join state.
    /// </summary>
    [JsonPropertyName("join_state")]
    public GroupJoinState JoinState { get; set; } = GroupJoinState.None;

    /// <summary>
    /// Time elapsed since last re-balance (assign or revoke) (milliseconds).
    /// </summary>
    [JsonPropertyName("rebalance_age")]
    public long RebalanceAgeMilliseconds { get; set; }

    /// <summary>
    /// Total number of re-balances (assign or revoke).
    /// </summary>
    [JsonPropertyName("rebalance_cnt")]
    public long RebalanceCount { get; set; }

    /// <summary>
    /// Last rebalance reason, or empty string.
    /// </summary>
    [JsonPropertyName("rebalance_reason")]
    public string RebalanceReason { get; set; } = string.Empty;
}