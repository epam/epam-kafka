// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Subscription.Options;
using Epam.Kafka.PubSub.Subscription.Topics;
using Epam.Kafka.PubSub.Utils;

namespace Epam.Kafka.PubSub.Subscription.State;

internal sealed class ExternalState<TOffsetsStorage> : BatchState
    where TOffsetsStorage : IExternalOffsetsStorage
{
    private readonly TOffsetsStorage _offsetsStorage;

    public ExternalState(TOffsetsStorage offsetsStorage)
    {
        this._offsetsStorage = offsetsStorage ?? throw new ArgumentNullException(nameof(offsetsStorage));
    }

    protected override void AssignConsumer<TKey, TValue>(SubscriptionTopicWrapper<TKey, TValue> topic,
        CancellationToken cancellationToken)
    {
        if (topic == null)
            throw new ArgumentNullException(nameof(topic));

        IReadOnlyCollection<TopicPartition> topicPartitions = topic.Options.GetTopicPartitions();

        IReadOnlyCollection<TopicPartitionOffset> state =
            topic.GetAndResetState(this._offsetsStorage, topicPartitions, cancellationToken);

        var reset = new List<TopicPartitionOffset>();
        var assign = new List<TopicPartitionOffset>();

        foreach (TopicPartitionOffset item in state)
        {
            // existing assignment, check if offset reset
            if (topic.Consumer.Assignment.Contains(item.TopicPartition))
            {
                if (topic.Offsets[item.TopicPartition] != item.Offset)
                {
                    TopicPartitionOffset tpo = new(item.TopicPartition, item.Offset);
                    topic.Consumer.Seek(tpo);
                    topic.Offsets[item.TopicPartition] = item.Offset;

                    reset.Add(tpo);
                }
            }
            else
            {
                TopicPartitionOffset tpo = new(item.TopicPartition, item.Offset);
                topic.Offsets[item.TopicPartition] = item.Offset;

                assign.Add(tpo);
            }
        }

        if (assign.Count > 0)
        {
            topic.Consumer.Assign(assign);

            topic.Logger.PartitionsAssigned(topic.Monitor.Name, null, assign);
        }

        topic.OnReset(reset);
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