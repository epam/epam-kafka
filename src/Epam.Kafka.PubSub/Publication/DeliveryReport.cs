// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

namespace Epam.Kafka.PubSub.Publication;

/// <summary>
///     Represent result of message delivery.
/// </summary>
public class DeliveryReport : TopicPartitionOffsetError
{
    /// <summary>
    ///     Initializes a new instance of <see cref="DeliveryReport" />.
    /// </summary>
    /// <param name="topic">The topic associated with the message.</param>
    /// <param name="partition">he partition associated with the message.</param>
    /// <param name="offset">The partition offset associated with the message.</param>
    /// <param name="error">An error (or NoError) associated with the message.</param>
    /// <param name="timestamp">The  Kafka timestamp associated with the message.</param>
    /// <param name="leaderEpoch">
    ///     The offset leader epoch (optional).
    /// </param>
    /// <param name="status">The persistence status of the message</param>
    public DeliveryReport(string topic, Partition partition, Offset offset, Error error, PersistenceStatus status,
        Timestamp timestamp, int? leaderEpoch = null) : base(topic, partition, offset, error, leaderEpoch)
    {
        this.Status = status;
        this.Timestamp = timestamp;
    }

    /// The
    /// <see cref="PersistenceStatus" />
    /// .
    public PersistenceStatus Status { get; }

    /// The
    /// <see cref="Timestamp" />
    /// .
    public Timestamp Timestamp { get; }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{base.ToString()} {this.Status}";
    }

    /// <summary>
    ///     Create <see cref="DeliveryReport" /> instance from <see cref="DeliveryReport{TKey,TValue}" />.
    /// </summary>
    /// <typeparam name="TKey">The original report key type.</typeparam>
    /// <typeparam name="TValue">The original report value type.</typeparam>
    /// <param name="x">The original report</param>
    /// <returns>The <see cref="DeliveryReport" /> instance.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static DeliveryReport FromGenericReport<TKey, TValue>(DeliveryReport<TKey, TValue> x)
    {
        if (x == null)
        {
            throw new ArgumentNullException(nameof(x));
        }

        return new(x.Topic, x.Partition, x.Offset, x.Error, x.Status, x.Timestamp);
    }
}