// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

namespace Epam.Kafka.PubSub.Subscription;

/// <summary>
///     Used by subscription background service to manage committed offsets.
/// </summary>
public interface IExternalOffsetsStorage
{
    /// <summary>
    ///     Invoked to commit offsets represented by <paramref name="offsets" /> or identify that committed offsets were
    ///     changed externally.
    /// </summary>
    /// <remarks>
    ///     The <see cref="TopicPartitionOffset" /> from <paramref name="offsets" /> equal to corresponding returned offset if
    ///     value was not externally modified during processing,
    ///     otherwise it should represent latest value to start next batch with.
    /// </remarks>
    /// <param name="offsets">The list of <see cref="TopicPartitionOffset" /> to commit.</param>
    /// <param name="consumerGroup">The consumer group value used for consumer.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken" />.</param>
    /// <returns></returns>
    IReadOnlyCollection<TopicPartitionOffset> CommitOrReset(
        IReadOnlyCollection<TopicPartitionOffset> offsets,
        string? consumerGroup,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Invoked to get existing committed offsets for topic partitions or create them.
    /// </summary>
    /// <param name="topics">The list of <see cref="TopicPartition" /> to get offsets for.</param>
    /// <param name="consumerGroup">The consumer group value used for consumer.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken" />.</param>
    /// <returns>The <see cref="TopicPartitionOffset" /> items corresponding to <paramref name="topics" />.</returns>
    IReadOnlyCollection<TopicPartitionOffset> GetOrCreate(
        IReadOnlyCollection<TopicPartition> topics,
        string? consumerGroup,
        CancellationToken cancellationToken);
}