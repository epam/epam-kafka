﻿// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.Tests.Common;

using Microsoft.Extensions.Logging;

#if EF6
using Epam.Kafka.PubSub.EntityFramework6.Subscription;

using System.Data.Entity;

namespace Epam.Kafka.PubSub.EntityFramework6.Tests.Helpers;
#else
using Epam.Kafka.PubSub.EntityFrameworkCore.Subscription;
using Microsoft.EntityFrameworkCore;

namespace Epam.Kafka.PubSub.EntityFrameworkCore.Tests.Helpers;
#endif

public class
    TestDbContextEntitySubscriptionHandler : DbContextEntitySubscriptionHandler<string, TestEntityKafka, TestContext,
        TestEntityDb>
{
    public TestDbContextEntitySubscriptionHandler(TestContext context,
        ILogger<TestDbContextEntitySubscriptionHandler> logger) : base(context, logger)
    {
    }

    protected override bool IsDeleted(ConsumeResult<string, TestEntityKafka> value)
    {
        return false;
    }

    protected override void LoadMainChunk(IQueryable<TestEntityDb> queryable,
        IReadOnlyCollection<ConsumeResult<string, TestEntityKafka>> chunk)
    {
        IEnumerable<string> keys = chunk.Select(v => v.Message.Key);

        queryable.Where(x => keys.Contains(x.ExternalId)).Load();
    }

    protected override TestEntityDb? FindLocal(DbSet<TestEntityDb> dbSet, ConsumeResult<string, TestEntityKafka> value)
    {
        return dbSet.SingleOrDefault(x => x.ExternalId == value.Message.Key);
    }

    protected override string Update(ConsumeResult<string, TestEntityKafka> value, TestEntityDb entity, bool created)
    {
        entity.Name = value.Message.Value.Name;

        return "Updated";
    }

    protected override bool TryCreate(ConsumeResult<string, TestEntityKafka> value, out TestEntityDb? entity)
    {
        entity = new TestEntityDb { ExternalId = value.Message.Key };

        return true;
    }
}