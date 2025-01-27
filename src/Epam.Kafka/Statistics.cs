// Copyright © 2024 EPAM Systems

using Epam.Kafka.Stats;

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
    /// Name of <see cref="Meter"/> used to expose top level statistics.
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
            result = JsonSerializer.Deserialize(json, StatsJsonContext.Default.Statistics) ??
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
    /// Instance type (producer or consumer).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// librdkafka's internal monotonic clock (microseconds).
    /// </summary>
    [JsonPropertyName("ts")]
    public long ClockMicroseconds { get; set; }

    /// <summary>
    /// Wall clock time in seconds since the epoch.
    /// </summary>
    [JsonPropertyName("time")]
    public long TimeEpochSeconds { get; set; }

    /// <summary>
    /// Time since this client instance was created (microseconds).
    /// </summary>
    [JsonPropertyName("age")]
    public long AgeMicroseconds { get; set; }

    /// <summary>
    /// Number of ops (callbacks, events, etc) waiting in queue for application to serve with rd_kafka_poll().
    /// </summary>
    /// <remarks>Integer gauge (64 bits wide). Will be reset to 0 on each stats emit.</remarks>
    [JsonPropertyName("replyq")]
    public long OpsQueueCountGauge { get; set; }

    /// <summary>
    /// Current number of messages in producer queues.
    /// </summary>
    /// <remarks>Integer gauge (64 bits wide). Will be reset to 0 on each stats emit.</remarks>
    [JsonPropertyName("msg_cnt")]
    public long ProducerQueueCountGauge { get; set; }

    /// <summary>
    /// Current total size of messages in producer queues.
    /// </summary>
    /// <remarks>Integer gauge (64 bits wide). Will be reset to 0 on each stats emit.</remarks>
    [JsonPropertyName("msg_size")]
    public long ProducerQueueSizeGauge { get; set; }

    /// <summary>
    /// Threshold: maximum number of messages allowed on the producer queues.
    /// </summary>
    /// <remarks> Integer counter (64 bits wide). Ever increasing.</remarks>
    [JsonPropertyName("msg_max")]
    public long ProducerQueueMax { get; set; }

    /// <summary>
    /// Threshold: maximum total size of messages allowed on the producer queues.
    /// </summary>
    /// <remarks> Integer counter (64 bits wide). Ever increasing.</remarks>
    [JsonPropertyName("msg_size_max")]
    public long ProducerQueueSizeMax { get; set; }

    //TODO: tx, tx_bytes, rx, rx_bytes

    /// <summary>
    /// Total number of messages transmitted (produced) to Kafka brokers.
    /// </summary>
    [JsonPropertyName("txmsgs")]
    public long TransmittedMessagesTotal { get; set; }

    //TODO: txmsg_bytes

    /// <summary>
    /// Total number of messages consumed, not including ignored messages (due to offset, etc), from Kafka brokers.
    /// </summary>
    [JsonPropertyName("rxmsgs")]
    public long ConsumedMessagesTotal { get; set; }

    //TODO: rxmsg_bytes, simple_cnt, metadata_cache_cnt

    /// <summary>
    /// Raw json string representation emitted by handler.
    /// </summary>
    [JsonIgnore]
    public string RawJson { get; private set; } = null!;

    /// <summary>
    /// Dict of brokers, key is broker name, value is <see cref="BrokerStatistics"/>.
    /// </summary>
    [JsonPropertyName("brokers")]
    public Dictionary<string, BrokerStatistics> Brokers { get; } = new();

    /// <summary>
    /// Dict of topics, key is topic name, value is <see cref="TopicStatistics"/>.
    /// </summary>
    [JsonPropertyName("topics")]
    public Dictionary<string, TopicStatistics> Topics { get; } = new();

    /// <summary>
    /// Consumer group metrics. See <see cref="GroupStatistics"/>.
    /// </summary>
    [JsonPropertyName("cgrp")]
    public GroupStatistics? ConsumerGroups { get; set; }

    //TODO: eos
}