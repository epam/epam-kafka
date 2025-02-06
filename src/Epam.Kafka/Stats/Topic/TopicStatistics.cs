// Copyright © 2024 EPAM Systems

using System.Text.Json.Serialization;

namespace Epam.Kafka.Stats.Topic;

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

    /// <summary>
    /// Batch sizes in bytes
    /// </summary>
    [JsonPropertyName("batchsize")]
    public WindowStatistics BatchSize { get; } = new();

    /// <summary>
    /// Batch message counts
    /// </summary>
    [JsonPropertyName("batchcnt")]
    public WindowStatistics BatchCount { get; } = new();

    /// <summary>
    /// Partitions dict, key is partition id, value is <see cref="PartitionStatistics"/>
    /// </summary>
    [JsonPropertyName("partitions")]
    public Dictionary<long, PartitionStatistics> Partitions { get; } = new();
}