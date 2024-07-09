// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Subscription.Topics;
using Epam.Kafka.PubSub.Utils;

namespace Epam.Kafka.PubSub.Subscription.State;

internal static class ExternalStateExtensions
{
    public static IReadOnlyCollection<TopicPartitionOffset> CommitState<TKey, TValue>(
        this IExternalOffsetsStorage offsetsStorage,
        SubscriptionTopicWrapper<TKey, TValue> topic,
        IReadOnlyCollection<TopicPartitionOffset> offsets,
        ActivityWrapper activitySpan,
        CancellationToken cancellationToken)
    {
        if (topic == null)
            throw new ArgumentNullException(nameof(topic));

        if (offsets == null)
            throw new ArgumentNullException(nameof(offsets));

        var reset = new Dictionary<TopicPartitionOffset, TopicPartitionOffset?>();
        var committed = new List<TopicPartitionOffset>();
        IReadOnlyCollection<TopicPartitionOffset> newState;

        using (ActivityWrapper wrapper = activitySpan.CreateSpan("commit_external"))
        {
            newState = offsetsStorage.CommitOrReset(offsets, topic.ConsumerGroup, cancellationToken);
            wrapper.SetResult(newState);
        }

        foreach (TopicPartitionOffset item in newState)
        {
            TopicPartitionOffset expected = offsets.Single(x => x.TopicPartition == item.TopicPartition);

            // compare actual and expected value to understand if offset was committed or reset
            if (expected.Offset == item.Offset)
            {
                committed.Add(expected);
            }
            else
            {
                TopicPartitionOffset tpo = new(item.TopicPartition, item.Offset);

                reset.Add(tpo,
                    topic.Offsets.TryGetValue(item.TopicPartition, out var old)
                        ? new TopicPartitionOffset(item.TopicPartition, old)
                        : null);

                topic.Seek(tpo);
            }
        }

        topic.OnReset(reset);

        if (topic.Options.ExternalStateCommitToKafka)
        {
            try
            {
                topic.CommitOffsets(activitySpan, committed);
            }
            catch (KafkaException exception)
            {
                topic.Logger.KafkaCommitFailed(exception, topic.Monitor.Name, committed);

                // ignore exception because external provider is a single point of truth for offsets.
                // failed commit will trigger offset reset on next batch iteration
            }
        }

        return committed;
    }
}