﻿// Copyright © 2024 EPAM Systems

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

        var pause = new List<TopicPartition>();
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
                PauseOrReset(topic, item, pause, reset);
            }
        }

        topic.OnReset(reset);

        topic.OnPause(pause);

        topic.CommitOffsetIfNeeded(activitySpan, newState);

        return committed;
    }

    public static void PauseOrReset<TKey, TValue>(
        SubscriptionTopicWrapper<TKey, TValue> topic,
        TopicPartitionOffset item,
        List<TopicPartition> pause,
        List<TopicPartitionOffset> reset)
    {
        if (item.Offset == ExternalOffset.Paused)
        {
            pause.Add(item.TopicPartition);
        }
        else
        {
            TopicPartitionOffset tpo = new(item.TopicPartition, item.Offset);
            topic.Seek(tpo);
            reset.Add(tpo);
        }
    }

    public static void CommitOffsetIfNeeded<TKey, TValue>(
        this SubscriptionTopicWrapper<TKey, TValue> topic,
        ActivityWrapper activitySpan,
        IEnumerable<TopicPartitionOffset> offsets)
    {
        if (topic.Options.ExternalStateCommitToKafka)
        {
            List<TopicPartitionOffset> toCommit = new();

            try
            {
                foreach (TopicPartitionOffset item in offsets)
                {
                    if (item.Offset.Value >= 0)
                    {
                        toCommit.Add(item);
                    }
                    else if (item.Offset == Offset.Beginning)
                    {
                        WatermarkOffsets w = topic.Consumer.GetWatermarkOffsets(item.TopicPartition);

                        if (w.Low == Offset.Unset)
                        {
                            w = topic.Consumer.QueryWatermarkOffsets(item.TopicPartition, TimeSpan.FromSeconds(5));
                        }

                        if (w.Low.Value >= 0)
                        {
                            toCommit.Add(new TopicPartitionOffset(item.TopicPartition, w.Low));
                        }
                    }
                    else if (item.Offset == Offset.End)
                    {
                        WatermarkOffsets w = topic.Consumer.QueryWatermarkOffsets(item.TopicPartition, TimeSpan.FromSeconds(5));
                        toCommit.Add(new TopicPartitionOffset(item.TopicPartition, w.High));
                    }
                }

                topic.CommitOffsets(activitySpan, toCommit);
            }
            catch (KafkaException exception)
            {
                topic.Logger.KafkaCommitFailed(exception, topic.Monitor.Name, toCommit);

                // ignore exception because external provider is a single point of truth for offsets.
                // failed commit will trigger offset reset on next batch iteration
            }
        }
    }
}