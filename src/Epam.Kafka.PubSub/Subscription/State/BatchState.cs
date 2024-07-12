// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Subscription.Topics;
using Epam.Kafka.PubSub.Utils;

namespace Epam.Kafka.PubSub.Subscription.State;

internal abstract class BatchState
{
    public bool GetBatch<TKey, TValue>(
        SubscriptionTopicWrapper<TKey, TValue> topic,
        ActivityWrapper activitySpan,
        out IReadOnlyCollection<ConsumeResult<TKey, TValue>> batch,
        CancellationToken cancellationToken)
    {
        if (topic == null)
            throw new ArgumentNullException(nameof(topic));

        topic.ClearIfNotAssigned();

        bool unassignedBeforeRead;

        using (var span = activitySpan.CreateSpan("assign"))
        {
            this.AssignConsumer(topic, span, cancellationToken);
        }

        do
        {
            topic.Rebalanced = false;
            cancellationToken.ThrowIfCancellationRequested();

            unassignedBeforeRead = topic.Consumer.Assignment.Count == 0;

            try
            {
                batch = topic.GetBatch(activitySpan, cancellationToken);

                // try to throw handler assign exception after read to be able to do it after potential re-balance in same batch.
                topic.ThrowIfNeeded();
            }
            catch (Exception exception)
            {
                if (exception.RetryBatchAllowed() && topic.Rebalanced)
                {
                    var toPause = topic.Offsets.Where(x => x.Value == ExternalOffset.Paused).Select(x => x.Key).ToArray();

                    topic.OnPause(toPause);
                }

                throw;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (topic.Rebalanced)
            {
                var toPause = topic.Offsets.Where(x => x.Value == ExternalOffset.Paused).Select(x => x.Key).ToArray();

                bool affected = topic.OnPause(toPause);

                if (!affected)
                {
                    break;
                }
            }

        } while (topic.Rebalanced);

        return unassignedBeforeRead;
    }

    protected abstract void AssignConsumer<TKey, TValue>(SubscriptionTopicWrapper<TKey, TValue> topic,
        ActivityWrapper activitySpan,
        CancellationToken cancellationToken);

    public void CommitResults<TKey, TValue>(SubscriptionTopicWrapper<TKey, TValue> topic,
        ActivityWrapper activitySpan,
        IReadOnlyCollection<TopicPartitionOffset> offsets,
        CancellationToken cancellationToken)
    {
        if (topic == null)
            throw new ArgumentNullException(nameof(topic));

        if (offsets == null)
            throw new ArgumentNullException(nameof(offsets));

        if (offsets.Count > 0)
        {
            IReadOnlyCollection<TopicPartitionOffset> committed =
                this.CommitState(topic, offsets, activitySpan, cancellationToken);

            topic.OnCommit(committed);
        }
    }

    protected abstract IReadOnlyCollection<TopicPartitionOffset> CommitState<TKey, TValue>(
        SubscriptionTopicWrapper<TKey, TValue> topic,
        IReadOnlyCollection<TopicPartitionOffset> offsets,
        ActivityWrapper activitySpan,
        CancellationToken cancellationToken);
}