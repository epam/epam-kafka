// Copyright © 2024 EPAM Systems

using Epam.Kafka.Internals;

using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Epam.Kafka;

/// <summary>
/// Represent statistic emitted by librdkafka. See https://github.com/confluentinc/librdkafka/blob/master/STATISTICS.md for details.
/// </summary>
public class Statistics
{
    /// <summary>
    /// Name of <see cref="Meter"/> used to expose statistics if <see cref="KafkaConfigExtensions.DotnetStatisticMetricsKey"/> enable it.
    /// </summary>
    public const string MeterName = "Epam.Kafka.Statistics";

    /// <summary>
    /// Create new instance of <see cref="Statistics"/> object from json representation.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static Statistics FromJson(string json)
    {
        if (json == null) throw new ArgumentNullException(nameof(json));

        Statistics? result;
        try
        {
            result = JsonSerializer.Deserialize(json, JsonContext.Default.Statistics) ??
                     throw new ArgumentException("Json deserialized to null value", nameof(json));
            result.RawJson = json;
        }
        catch (JsonException jsonException)
        {
            throw new ArgumentException("Unable to deserialize json, see inner exception for details.",
                nameof(json), jsonException);
        }

        return result;
    }

    /// <summary>
    /// Instance type (producer or consumer)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Handle instance name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The configured (or default) client.id
    /// </summary>
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = "rdkafka";

    /// <summary>
    /// Total number of messages transmitted (produced) to Kafka brokers
    /// </summary>
    [JsonPropertyName("txmsgs")]
    public long TransmittedMessagesTotal { get; set; }

    /// <summary>
    /// Total number of messages consumed, not including ignored messages (due to offset, etc), from Kafka brokers
    /// </summary>
    [JsonPropertyName("rxmsgs")]
    public long ConsumedMessagesTotal { get; set; }

    /// <summary>
    /// Raw json string representation emitted by handler.
    /// </summary>
    [JsonIgnore]
    public string RawJson { get; private set; } = null!;

    /// <summary>
    /// Time since this client instance was created (microseconds)
    /// </summary>
    [JsonPropertyName("age")]
    public long AgeMicroseconds { get; set; }

#pragma warning disable CA2227 // Required for json deserialization
    /// <summary>
    /// Dict of brokers, key is broker name, value is <see cref="BrokerStatistics"/>
    /// </summary>
    [JsonPropertyName("brokers")]
    public Dictionary<string, BrokerStatistics> Brokers { get; set; } = new();

    /// <summary>
    /// Dict of topics, key is topic name, value is <see cref="TopicStatistics"/>
    /// </summary>
    [JsonPropertyName("topics")]
    public Dictionary<string, TopicStatistics> Topics { get; set; } = new();
#pragma warning restore CA2227
}

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
}

/// <summary>
/// Topic statistics. See https://github.com/confluentinc/librdkafka/blob/master/STATISTICS.md for details.
/// </summary>
public class TopicStatistics
{
    /// <summary>
    /// Topic name
    /// </summary>
    [JsonPropertyName("topic")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Age of client's topic object (milliseconds)
    /// </summary>
    [JsonPropertyName("age")]
    public long AgeMilliseconds { get; set; }

    /// <summary>
    /// Age of metadata from broker for this topic (milliseconds)
    /// </summary>
    [JsonPropertyName("metadata_age")]
    public long MetadataAgeMilliseconds { get; set; }

#pragma warning disable CA2227 // Required for json deserialization
    /// <summary>
    /// Partitions dict, key is partition id, value is <see cref="PartitionStatistics"/>
    /// </summary>
    [JsonPropertyName("partitions")]
    public Dictionary<long, PartitionStatistics> Partitions { get; set; } = new();
#pragma warning restore CA2227
}

/// <summary>
/// Partition statistics. See https://github.com/confluentinc/librdkafka/blob/master/STATISTICS.md for details.
/// </summary>
public class PartitionStatistics
{
    /// <summary>
    ///  Internal UA/UnAssigned partition
    /// </summary>
    public const long InternalUnassignedPartition = -1;

    /// <summary>
    /// Partition Id (-1 for internal UA/UnAssigned partition)
    /// </summary>
    [JsonPropertyName("partition")]
    public long Id { get; set; }

    /// <summary>
    /// Consumer fetch state for this partition (none, stopping, stopped, offset-query, offset-wait, active)
    /// </summary>
    [JsonPropertyName("fetch_state")]
    public string FetchState { get; set; } = string.Empty;

    /// <summary>
    /// Next offset to fetch
    /// </summary>
    [JsonPropertyName("next_offset")]
    public long NextOffset { get; set; }

    /// <summary>
    /// Last committed offset
    /// </summary>
    [JsonPropertyName("committed_offset")]
    public long CommittedOffset { get; set; }

    /// <summary>
    /// Difference between (hi_offset or ls_offset) and committed_offset). hi_offset is used when isolation.level=read_uncommitted, otherwise ls_offset.
    /// </summary>
    [JsonPropertyName("consumer_lag")]
    public long ConsumerLag { get; set; }

    /// <summary>
    /// Partition's high watermark offset on broker
    /// </summary>
    [JsonPropertyName("hi_offset")]
    public long HiOffset { get; set; }

    /// <summary>
    /// Partition's last stable offset on broker, or same as hi_offset is broker version is less than 0.11.0.0
    /// </summary>
    [JsonPropertyName("ls_offset")]
    public long LsOffset { get; set; }

    /// <summary>
    /// Partition's low watermark offset on broker
    /// </summary>
    [JsonPropertyName("lo_offset")]
    public long LoOffset { get; set; }

}