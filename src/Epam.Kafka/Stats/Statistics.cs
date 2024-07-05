﻿// Copyright © 2024 EPAM Systems

using Epam.Kafka.Internals;

using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Epam.Kafka.Stats;

/// <summary>
/// Represent statistic emitted by librdkafka. See https://github.com/confluentinc/librdkafka/blob/master/STATISTICS.md for details.
/// </summary>
public class Statistics
{
    /// <summary>
    /// Name of <see cref="Meter"/> used to expose top level statistics if <see cref="KafkaConfigExtensions.DotnetStatisticMetricsKey"/> enable it.
    /// </summary>
    public const string MeterName = "Epam.Kafka.Statistics";

    /// <summary>
    /// Name of <see cref="Meter"/> used to expose topics statistics if <see cref="KafkaConfigExtensions.DotnetStatisticMetricsKey"/> enable it.
    /// </summary>
    public const string TopicsMeterName = "Epam.Kafka.Statistics.Topics";

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
    /// Instance type (producer or consumer).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Handle instance name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Wall clock time in seconds since the epoch.
    /// </summary>
    [JsonPropertyName("time")]
    public long EpochTimeSeconds { get; set; }

    /// <summary>
    /// The configured (or default) client.id
    /// </summary>
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = "rdkafka";

    /// <summary>
    /// Total number of messages transmitted (produced) to Kafka brokers.
    /// </summary>
    [JsonPropertyName("txmsgs")]
    public long TransmittedMessagesTotal { get; set; }

    /// <summary>
    /// Total number of messages consumed, not including ignored messages (due to offset, etc), from Kafka brokers.
    /// </summary>
    [JsonPropertyName("rxmsgs")]
    public long ConsumedMessagesTotal { get; set; }

    /// <summary>
    /// Raw json string representation emitted by handler.
    /// </summary>
    [JsonIgnore]
    public string RawJson { get; private set; } = null!;

    /// <summary>
    /// Time since this client instance was created (microseconds).
    /// </summary>
    [JsonPropertyName("age")]
    public long AgeMicroseconds { get; set; }

    /// <summary>
    /// Consumer group metrics. See <see cref="GroupStatistics"/>.
    /// </summary>
    [JsonPropertyName("cgrp")]
    public GroupStatistics? ConsumerGroups { get; set; }

#pragma warning disable CA2227 // Required for json deserialization
    /// <summary>
    /// Dict of brokers, key is broker name, value is <see cref="BrokerStatistics"/>.
    /// </summary>
    [JsonPropertyName("brokers")]
    public Dictionary<string, BrokerStatistics> Brokers { get; set; } = new();

    /// <summary>
    /// Dict of topics, key is topic name, value is <see cref="TopicStatistics"/>.
    /// </summary>
    [JsonPropertyName("topics")]
    public Dictionary<string, TopicStatistics> Topics { get; set; } = new();
#pragma warning restore CA2227
}