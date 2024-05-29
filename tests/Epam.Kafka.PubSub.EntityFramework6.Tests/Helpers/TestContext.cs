// Copyright © 2024 EPAM Systems

using System.Data.Common;
using System.Data.Entity;

using Epam.Kafka.PubSub.EntityFramework6.Subscription.State;

namespace Epam.Kafka.PubSub.EntityFramework6.Tests.Helpers;

public class TestContext : DbContext, IKafkaStateDbContext
{
    public TestContext(DbConnection connection) : base(connection, false)
    {
    }

    public DbSet<KafkaTopicState> KafkaTopicStates => this.Set<KafkaTopicState>();

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddKafkaState();

        modelBuilder.Entity<TestEntityDb>().HasKey(x => x.Id);
        modelBuilder.Entity<TestEntityDb>().HasIndex(x => x.ExternalId).IsUnique(true);
    }
}