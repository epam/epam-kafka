// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Utils;

using System.Diagnostics;

namespace Epam.Kafka.PubSub.Publication.Topics;

internal interface IPublicationTopicWrapper<TKey, TValue> : IDisposable
{
    bool RequireTransaction { get; }
    DateTimeOffset? TransactionEnd { get; }
    void CommitTransactionIfNeeded(ActivityWrapper apm);
    void AbortTransactionIfNeeded(ActivityWrapper apm);

    IDictionary<TopicMessage<TKey, TValue>, DeliveryReport> Produce(
        IReadOnlyCollection<TopicMessage<TKey, TValue>> items,
        ActivityWrapper activitySpan,
        Stopwatch stopwatch,
        TimeSpan handlerTimeout,
        CancellationToken cancellationToken);
}