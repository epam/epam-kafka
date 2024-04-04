// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka.PubSub.EntityFrameworkCore.Subscription.State;
using Epam.Kafka.PubSub.Subscription;

using Microsoft.EntityFrameworkCore;

namespace Epam.Kafka.PubSub.EntityFrameworkCore.Subscription;

internal sealed class DbContextOffsetsStorage<TContext> : IExternalOffsetsStorage
    where TContext : DbContext, IKafkaStateDbContext
{
    private readonly TContext _context;
    private bool _committed;

    public DbContextOffsetsStorage(TContext context)
    {
        this._context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IReadOnlyCollection<TopicPartitionOffset> GetOrCreate(IReadOnlyCollection<TopicPartition> topics,
        string? consumerGroup, CancellationToken cancellationToken)
    {
        if (topics == null)
            throw new ArgumentNullException(nameof(topics));

        consumerGroup ??= string.Empty;

        List<TopicPartitionOffset> result =
            this.GetLocalState(topics, consumerGroup, out List<TopicPartition> toRequest);

        if (toRequest.Count > 0)
        {
            foreach (IGrouping<string, TopicPartition> g in toRequest.GroupBy(x => x.Topic))
            {
                int[] partitions = g.Select(tp => tp.Partition.Value).ToArray();

                this._context.KafkaTopicStates.AsTracking().Where(x =>
                    x.ConsumerGroup == consumerGroup && x.Topic == g.Key && partitions.Contains(x.Partition)
                ).Load();
            }
        }

        foreach (TopicPartition item in toRequest)
        {
            KafkaTopicState? local = this._context.KafkaTopicStates.Local.SingleOrDefault(x =>
                x.Topic == item.Topic && x.Partition == item.Partition && x.ConsumerGroup == consumerGroup);

            if (local != null)
                result.Add(new TopicPartitionOffset(item, local.Pause ? Offset.End : local.Offset));
            else
            {
                // don't have information in database yet;
                result.Add(new TopicPartitionOffset(item, Offset.Unset));
                this._context.KafkaTopicStates.Add(new KafkaTopicState
                {
                    Topic = item.Topic,
                    Partition = item.Partition,
                    ConsumerGroup = consumerGroup,
                    Offset = Offset.Unset,
                    Timestamp = DateTimeOffset.UtcNow
                });
            }
        }

        return result;
    }

    public IReadOnlyCollection<TopicPartitionOffset> CommitOrReset(IReadOnlyCollection<TopicPartitionOffset> offsets,
        string? consumerGroup,
        CancellationToken cancellationToken)
    {
        if (offsets == null)
            throw new ArgumentNullException(nameof(offsets));

        consumerGroup ??= string.Empty;

        if (this._committed)
            throw new InvalidOperationException("CommitOrReset was already been executed");

        this._committed = true;

        foreach (TopicPartitionOffset item in offsets)
        {
            KafkaTopicState local = this._context.KafkaTopicStates.Local.Single(x =>
                x.Topic == item.Topic && x.Partition == item.Partition && x.ConsumerGroup == consumerGroup);

            if (local.Pause && item.Offset == Offset.End)
            {
                // don't update paused topics with standard Offset.End value
            }
            else
            {
                local.Offset = item.Offset;
            }
        }

        try
        {
            this._context.SaveChanges(true);
        }
        catch (DbUpdateException exception)
        {
            foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry in exception.Entries)
            {
                if (entry.Entity is KafkaTopicState)
                {
                    Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues proposedValues = entry.CurrentValues;
                    Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues? databaseValues =
                        entry.GetDatabaseValues();

                    if (databaseValues != null)
                    {
                        foreach (Microsoft.EntityFrameworkCore.Metadata.IProperty property in proposedValues.Properties)
                        {
                            proposedValues[property] = databaseValues[property];
                        }

                        // Refresh original values to bypass next concurrency check
                        entry.OriginalValues.SetValues(databaseValues);
                    }
                }
            }

            this._context.SaveChanges(true);
        }

        return this.GetLocalState(offsets.Select(x => x.TopicPartition).ToList(), consumerGroup, out _);
    }

    private List<TopicPartitionOffset> GetLocalState(IReadOnlyCollection<TopicPartition> topics, string? consumerGroup,
        out List<TopicPartition> toRequest)
    {
        var result = new List<TopicPartitionOffset>(topics.Count);

        toRequest = new List<TopicPartition>(topics.Count);

        foreach (TopicPartition item in topics)
        {
            KafkaTopicState? local = this._context.KafkaTopicStates.Local.SingleOrDefault(x =>
                x.Topic == item.Topic && x.Partition == item.Partition && x.ConsumerGroup == consumerGroup);

            if (local != null)
                result.Add(new TopicPartitionOffset(item, local.Pause ? Offset.End : local.Offset));
            else
            {
                toRequest.Add(item);
            }
        }

        return result;
    }
}