// Copyright © 2024 EPAM Systems

using System.ComponentModel.DataAnnotations;

#if NET462 || NETSTANDARD2_1
namespace Epam.Kafka.PubSub.EntityFramework6.Subscription.State;
#else
namespace Epam.Kafka.PubSub.EntityFrameworkCore.Subscription.State;
#endif

/// <summary>
///     Represent subscription processing state (offsets for unique combination of topic name, consumer group name,
///     partition).
/// </summary>
public class KafkaTopicState
{
    /// <summary>
    ///     The topic name
    /// </summary>
    [Required]
    public string? Topic { get; set; }

    /// <summary>
    ///     The consumer group name.
    /// </summary>
    [Required]
    public string ConsumerGroup { get; set; } = string.Empty;

    /// <summary>
    ///     The partition value.
    /// </summary>
    [Range(0, int.MaxValue)]
    [Required]
    public int Partition { get; set; }

    /// <summary>
    ///     The committed offset value.
    /// </summary>
    [ConcurrencyCheck]
    [Required]
    public long Offset { get; set; }

    /// <summary>
    ///     Whether subscription processing paused.
    /// </summary>
    [Required]
    public bool Pause { get; set; }

    /// <summary>
    ///     Timestamp of last update.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTime.UtcNow;
}