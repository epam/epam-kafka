// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Subscription.Options;
using Epam.Kafka.PubSub.Subscription.Topics;
using Epam.Kafka.PubSub.Utils;

namespace Epam.Kafka.PubSub.Subscription.State;

internal class InternalKafkaState : BatchState
{
    protected override void AssignConsumer<TKey, TValue>(SubscriptionTopicWrapper<TKey, TValue> topic,
        ActivityWrapper activitySpan,
        CancellationToken cancellationToken)
    {
        if (topic == null)
            throw new ArgumentNullException(nameof(topic));

        if (topic.Consumer.Subscription.Count == 0)
        {
            topic.Consumer.Subscribe(topic.Options.GetTopicNames());
        }
    }

    protected override IReadOnlyCollection<TopicPartitionOffset> CommitState<TKey, TValue>(
        SubscriptionTopicWrapper<TKey, TValue> topic,
        IReadOnlyCollection<TopicPartitionOffset> offsets,
        ActivityWrapper activitySpan,
        CancellationToken cancellationToken)
    {
        if (topic == null)
            throw new ArgumentNullException(nameof(topic));

        try
        {
            topic.CommitOffsets(activitySpan, offsets);

            return offsets;
        }
        catch (KafkaException exc)
        {
            // unable to commit most likely because leaving group. Subscribe again is the only 1 option.
            topic.Logger.OffsetsCommitError(exc, topic.Monitor.Name, offsets);

            topic.Consumer.Unsubscribe();

            return Array.Empty<TopicPartitionOffset>();
        }
    }
}