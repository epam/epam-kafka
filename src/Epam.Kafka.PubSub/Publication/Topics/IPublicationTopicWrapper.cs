// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Utils;

using System.Diagnostics;
using Confluent.Kafka;

namespace Epam.Kafka.PubSub.Publication.Topics;

internal interface IPublicationTopicWrapper<TKey, TValue> : IDisposable
{
    bool Disposed { get; }
    bool RequireTransaction { get; }
    DateTimeOffset? TransactionEnd { get; }
    void CommitTransactionIfNeeded(ActivityWrapper apm);
    void SendOffsetsToTransactionIfNeeded(ActivityWrapper apm, IConsumerGroupMetadata metadata, IReadOnlyCollection<TopicPartitionOffset> offsets);
    void AbortTransactionIfNeeded(ActivityWrapper apm);

    IDictionary<TopicMessage<TKey, TValue>, DeliveryReport> Produce(
        IReadOnlyCollection<TopicMessage<TKey, TValue>> items,
        ActivityWrapper activitySpan,
        Stopwatch stopwatch,
        TimeSpan handlerTimeout,
        CancellationToken cancellationToken);
}