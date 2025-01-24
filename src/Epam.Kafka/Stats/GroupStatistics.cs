// Copyright © 2024 EPAM Systems

using System.Text.Json.Serialization;

namespace Epam.Kafka.Stats;

/// <summary>
/// Consumer group metrics.
/// </summary>
public class GroupStatistics
{
    /// <summary>
    /// Local consumer group handler's state.
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Time elapsed since last state change (milliseconds).
    /// </summary>
    [JsonPropertyName("stateage")]
    public long StateAgeMilliseconds { get; set; }

    /// <summary>
    /// Local consumer group handler's join state.
    /// </summary>
    [JsonPropertyName("join_state")]
    public string JoinState { get; set; } = string.Empty;

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
}