// Copyright © 2024 EPAM Systems

using System.Text.Json.Serialization;

namespace Epam.Kafka.Stats.Topic;

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
    /// Partition is explicitly desired by application
    /// </summary>
    [JsonPropertyName("desired")]
    public bool Desired { get; set; }

    /// <summary>
    /// Partition not seen in topic metadata from broker
    /// </summary>
    [JsonPropertyName("unknown")]
    public bool Unknown { get; set; }

    /// <summary>
    /// Number of messages waiting to be produced in first-level queue
    /// </summary>
    [JsonPropertyName("msgq_cnt")]
    public long QueueCount { get; set; }

    /// <summary>
    /// Number of pre-fetched messages in fetch queue
    /// </summary>
    [JsonPropertyName("fetchq_cnt")]
    public long FetchCount { get; set; }

    /// <summary>
    /// Consumer fetch state for this partition
    /// </summary>
    [JsonPropertyName("fetch_state")]
    public PartitionFetchState FetchState { get; set; } = PartitionFetchState.None;

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