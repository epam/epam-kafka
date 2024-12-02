// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Subscription;

#if EF6
using Epam.Kafka.PubSub.EntityFramework6.Subscription.State;

using System.Data.Entity;
using System.Data.Entity.Infrastructure;
#else
using Epam.Kafka.PubSub.EntityFrameworkCore.Subscription.State;

using Microsoft.EntityFrameworkCore;
#endif

#if EF6
namespace Epam.Kafka.PubSub.EntityFramework6.Subscription;
#else
namespace Epam.Kafka.PubSub.EntityFrameworkCore.Subscription;
#endif

internal sealed class DbContextOffsetsStorage<TContext> : IExternalOffsetsStorage
    where TContext : DbContext, IKafkaStateDbContext
{
    private readonly TContext _context;

    public DbContextOffsetsStorage(TContext context)
    {
        this._context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IReadOnlyCollection<TopicPartitionOffset> GetOrCreate(IReadOnlyCollection<TopicPartition> topics,
        string? consumerGroup, CancellationToken cancellationToken)
    {
        if (topics == null)
        {
            throw new ArgumentNullException(nameof(topics));
        }

        consumerGroup ??= string.Empty;

        cancellationToken.ThrowIfCancellationRequested();
        DbSet<KafkaTopicState> dbSet = this._context.KafkaTopicStates;
        ICollection<KafkaTopicState> locals = dbSet.Local;

        List<TopicPartitionOffset> result =
            GetLocalState(locals, topics, consumerGroup, out List<TopicPartition> toRequest);

        if (toRequest.Count > 0)
        {
            foreach (IGrouping<string, TopicPartition> g in toRequest.GroupBy(x => x.Topic))
            {
                int[] partitions = g.Select(tp => tp.Partition.Value).ToArray();

                cancellationToken.ThrowIfCancellationRequested();

                dbSet.AsTracking()
                    .Where(x => x.ConsumerGroup == consumerGroup && x.Topic == g.Key && partitions.Contains(x.Partition))
                    .Load();
            }
        }

        foreach (TopicPartition item in toRequest)
        {
            KafkaTopicState? local = locals.SingleOrDefault(x =>
                x.Topic == item.Topic && x.Partition == item.Partition && x.ConsumerGroup == consumerGroup);

            if (local != null)
            {
                result.Add(new TopicPartitionOffset(item, local.Pause ? ExternalOffset.Paused : local.Offset));
            }
            else
            {
                // don't have information in database yet;
                result.Add(new TopicPartitionOffset(item, Offset.Unset));
                dbSet.Add(new KafkaTopicState
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

    public IReadOnlyCollection<TopicPartitionOffset> CommitOrReset(
        IReadOnlyCollection<TopicPartitionOffset> offsets,
        string? consumerGroup,
        CancellationToken cancellationToken)
    {
        if (offsets == null)
        {
            throw new ArgumentNullException(nameof(offsets));
        }

        consumerGroup ??= string.Empty;

        DbSet<KafkaTopicState> dbSet = this._context.KafkaTopicStates;
        ICollection<KafkaTopicState> locals = dbSet.Local;

        foreach (TopicPartitionOffset item in offsets)
        {
            KafkaTopicState local = locals.Single(x =>
                x.Topic == item.Topic && x.Partition == item.Partition && x.ConsumerGroup == consumerGroup);

            if (local.Pause && item.Offset == ExternalOffset.Paused)
            {
                // don't update paused topics with standard value.
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
            cancellationToken.ThrowIfCancellationRequested();

#pragma warning disable IDE0008 // Use explicit type not possible due to #if directives
            foreach (var entry in exception.Entries)
            {
                if (entry.Entity is KafkaTopicState)
                {
                    var proposedValues = entry.CurrentValues;
                    var databaseValues = entry.GetDatabaseValues();

                    if (databaseValues != null)
                    {
                        foreach (var property in
#if EF6
                                 proposedValues.PropertyNames
#else
                                 proposedValues.Properties
#endif
                                )
                        {
                            proposedValues[property] = databaseValues[property];
                        }

                        // Refresh original values to bypass next concurrency check
                        entry.OriginalValues.SetValues(databaseValues);
                    }
                }
            }
#pragma warning restore IDE0008

            this._context.SaveChanges(true);
        }

        return GetLocalState(locals, offsets.Select(x => x.TopicPartition).ToList(), consumerGroup, out _);
    }

    private static List<TopicPartitionOffset> GetLocalState(
        ICollection<KafkaTopicState> locals,
        IReadOnlyCollection<TopicPartition> topics,
        string? consumerGroup,
        out List<TopicPartition> toRequest)
    {
        var result = new List<TopicPartitionOffset>(topics.Count);

        toRequest = new List<TopicPartition>(topics.Count);

        foreach (TopicPartition item in topics)
        {
            KafkaTopicState? local = locals.SingleOrDefault(x =>
                x.Topic == item.Topic && x.Partition == item.Partition && x.ConsumerGroup == consumerGroup);

            if (local != null)
            {
                result.Add(new TopicPartitionOffset(item, local.Pause ? ExternalOffset.Paused : local.Offset));
            }
            else
            {
                toRequest.Add(item);
            }
        }

        return result;
    }
}