// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka.PubSub.Subscription.Topics;
using Epam.Kafka.PubSub.Utils;

namespace Epam.Kafka.PubSub.Subscription.State;

internal class CombinedState<TOffsetsStorage> : InternalKafkaState
    where TOffsetsStorage : IExternalOffsetsStorage
{
    private readonly TOffsetsStorage _offsetsStorage;

    public CombinedState(TOffsetsStorage offsetsStorage)
    {
        this._offsetsStorage = offsetsStorage ?? throw new ArgumentNullException(nameof(offsetsStorage));
    }

    protected override void AssignConsumer<TKey, TValue>(SubscriptionTopicWrapper<TKey, TValue> topic,
        CancellationToken cancellationToken)
    {
        topic.ExternalState = list => topic.GetAndResetState(this._offsetsStorage, list, cancellationToken);

        base.AssignConsumer(topic, cancellationToken);

        if (topic.Consumer.Assignment.Count > 0)
        {
            var reset = new List<TopicPartitionOffset>();

            IReadOnlyCollection<TopicPartitionOffset> state = this._offsetsStorage.GetOrCreate(
                topic.Consumer.Assignment, topic.ConsumerGroup,
                cancellationToken);

            foreach (TopicPartitionOffset item in state)
            {
                if (!topic.Offsets.TryGetValue(item.TopicPartition, out Offset previous))
                {
                    reset.Add(item);
                    continue; // Don't need to seek if previous offset unavailable
                }

                // don't reset paused offset
                if (previous != item.Offset)
                {
                    reset.Add(item);
                    topic.Consumer.Seek(item);
                }
            }

            topic.OnReset(reset);
        }
    }
    
    protected override IReadOnlyCollection<TopicPartitionOffset> CommitState<TKey, TValue>(
        SubscriptionTopicWrapper<TKey, TValue> topic,
        IReadOnlyCollection<TopicPartitionOffset> offsets,
        ActivityWrapper activitySpan,
        CancellationToken cancellationToken)
    {
        return this._offsetsStorage.CommitState(topic, offsets, activitySpan, cancellationToken);
    }
}