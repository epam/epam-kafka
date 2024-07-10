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
        {
            throw new ArgumentNullException(nameof(topic));
        }

        if (offsets == null)
        {
            throw new ArgumentNullException(nameof(offsets));
        }

        var reset = new List<TopicPartitionOffset>();
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

                topic.Seek(tpo);

                reset.Add(tpo);
            }
        }

        topic.OnReset(reset);

        topic.CommitOffsetIfNeeded(activitySpan, newState);

        return committed;
    }

    public static void CommitOffsetIfNeeded<TKey, TValue>(
        this SubscriptionTopicWrapper<TKey, TValue> topic, 
        ActivityWrapper activitySpan,
        IReadOnlyCollection<TopicPartitionOffset> offsets)
    {
        if (topic.Options.ExternalStateCommitToKafka)
        {
            try
            {
                List<TopicPartitionOffset> toCommit = new();

                foreach (TopicPartitionOffset item in offsets)
                {
                    if (item.Offset.Value >= 0)
                    {
                        toCommit.Add(item);
                    }
                    else if (item.Offset == Offset.Beginning)
                    {
                        var w = topic.Consumer.GetWatermarkOffsets(item.TopicPartition);

                        if (w.Low == Offset.Unset)
                        {
                            w = topic.Consumer.QueryWatermarkOffsets(item.TopicPartition, TimeSpan.FromSeconds(5));
                        }

                        if (w.Low.Value >= 0)
                        {
                            toCommit.Add(new TopicPartitionOffset(item.TopicPartition, w.Low));
                        }
                    }
                }

                topic.CommitOffsets(activitySpan, toCommit);
            }
            catch (KafkaException exception)
            {
                topic.Logger.KafkaCommitFailed(exception, topic.Monitor.Name, offsets);

                // ignore exception because external provider is a single point of truth for offsets.
                // failed commit will trigger offset reset on next batch iteration
            }
        }
    }
}