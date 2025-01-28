// Copyright © 2024 EPAM Systems

using System.Text.Json.Serialization;

namespace Epam.Kafka.Stats;

/// <summary>
/// Rolling window statistics. The values are in microseconds unless otherwise stated.
/// </summary>
public class WindowStats
{
    /// <summary>
    /// Smallest value
    /// </summary>
    [JsonPropertyName("min")]
    public long Min { get; set; }

    /// <summary>
    /// Largest value
    /// </summary>
    [JsonPropertyName("max")]
    public long Max { get; set; }

    /// <summary>
    /// Average value
    /// </summary>
    [JsonPropertyName("avg")]
    public long Avg { get; set; }

    /// <summary>
    /// Sum of values
    /// </summary>
    [JsonPropertyName("sum")]
    public long Sum { get; set; }

    /// <summary>
    /// Number of values sampled
    /// </summary>
    [JsonPropertyName("cnt")]
    public long Count { get; set; }
}