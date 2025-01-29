// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using System.Text.Json.Serialization;

namespace Epam.Kafka.Stats;

/// <summary>
/// Per broker statistics. See https://github.com/confluentinc/librdkafka/blob/master/STATISTICS.md for details.
/// </summary>
public class BrokerStatistics
{
    /// <summary>
    /// Broker hostname, port and broker id
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Broker id (-1 for bootstraps)
    /// </summary>
    [JsonPropertyName("nodeid")]
    public long NodeId { get; set; }

    /// <summary>
    /// Broker hostname (e.g. "example.com:9092").
    /// </summary>
    [JsonPropertyName("nodename")]
    public string NodeName { get; set; } = string.Empty;

    /// <summary>
    /// Broker source (learned, configured, internal, logical)
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Broker state (INIT, DOWN, CONNECT, AUTH, APIVERSION_QUERY, AUTH_HANDSHAKE, UP, UPDATE)
    /// </summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Time since last broker state change (microseconds)
    /// </summary>
    [JsonPropertyName("stateage")]
    public long StateAgeMicroseconds { get; set; }

    /// <summary>
    /// Partitions handled by this broker handle.
    /// </summary>
    [JsonPropertyName("toppars")]
    public Dictionary<string, TopicPartition> TopicPartitions { get; } = new();
}