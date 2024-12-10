// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Subscription;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.PubSub.Utils;

namespace Epam.Kafka.PubSub.Bridge;

internal interface ISubscriptionForPublicationHandler<TKey, TValue> : ISubscriptionHandler<TKey, TValue>
{
    void Execute(IReadOnlyCollection<ConsumeResult<TKey, TValue>> items, IServiceProvider sp,
        SubscriptionMonitor subscriptionMonitor,
        ActivityWrapper activityWrapper,
        IConsumerGroupMetadata? metadata, CancellationToken cancellationToken);
}