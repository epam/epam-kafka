// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.EntityFrameworkCore.Subscription;
using Epam.Kafka.Sample.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Epam.Kafka.Sample.Samples;

public class SubscriptionHandlerSample : DbContextEntitySubscriptionHandler<string, KafkaEntity?, SampleDbContext,
    SampleSubscriptionEntity>
{
    public SubscriptionHandlerSample(SampleDbContext context, ILogger<SubscriptionHandlerSample> logger) : base(context,
        logger)
    {
    }

    protected override bool IsDeleted(ConsumeResult<string, KafkaEntity?> value) => false;

    protected override void LoadMainChunk(IQueryable<SampleSubscriptionEntity> queryable,
        IReadOnlyCollection<ConsumeResult<string, KafkaEntity?>> chunk)
    {
        string[] ids = chunk.Select(x => x.Message.Key).ToArray();

        queryable.Where(x => ids.Contains(x.Id)).Load();
    }

    protected override SampleSubscriptionEntity? FindLocal(DbSet<SampleSubscriptionEntity> dbSet,
        ConsumeResult<string, KafkaEntity?> value) => dbSet.Find(value.Message.Key);

    protected override string? Update(ConsumeResult<string, KafkaEntity?> value, SampleSubscriptionEntity entity,
        bool created)
    {
        entity.Partition = value.Partition;
        entity.Offset = value.Offset;

        return null;
    }

    protected override bool TryCreate(ConsumeResult<string, KafkaEntity?> value, out SampleSubscriptionEntity? entity)
    {
        entity = new SampleSubscriptionEntity { Id = value.Message.Key };

        return true;
    }
}