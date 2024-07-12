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
        ActivityWrapper activitySpan,
        CancellationToken cancellationToken)
    {
        if (topic == null)
            throw new ArgumentNullException(nameof(topic));

        IReadOnlyCollection<TopicPartition> topicPartitions = topic.Options.GetTopicPartitions();

        IReadOnlyCollection<TopicPartitionOffset> state =
            topic.GetAndResetState(this._offsetsStorage, topicPartitions, cancellationToken);

        var pause = new List<TopicPartition>();
        var reset = new List<TopicPartitionOffset>();
        var assign = new List<TopicPartitionOffset>();
        var assignNonPaused = new List<TopicPartitionOffset>();

        foreach (TopicPartitionOffset item in state)
        {
            // existing assignment, check if offset reset
            if (topic.Consumer.Assignment.Contains(item.TopicPartition))
            {
                if (topic.Offsets[item.TopicPartition] != item.Offset)
                {
                    ExternalStateExtensions.PauseOrReset(topic, item, pause, reset);
                }
            }
            else
            {
                TopicPartitionOffset tpo = new(item.TopicPartition, item.Offset);

                // first assign offset.end, than pause consumer
                if (tpo.Offset == ExternalOffset.Paused)
                {
                    pause.Add(item.TopicPartition);
                    tpo = new(item.TopicPartition, Offset.End);
                }
                else
                {
                    assignNonPaused.Add(tpo);
                }

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

        topic.OnPause(pause);

        topic.CommitOffsetIfNeeded(activitySpan, reset.Concat(assignNonPaused));
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