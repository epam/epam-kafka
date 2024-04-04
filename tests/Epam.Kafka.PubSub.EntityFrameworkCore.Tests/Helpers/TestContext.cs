// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.EntityFrameworkCore.Subscription.State;

using Microsoft.EntityFrameworkCore;

namespace Epam.Kafka.PubSub.EntityFrameworkCore.Tests.Helpers;

public class TestContext : DbContext, IKafkaStateDbContext
{
    public TestContext(DbContextOptions<TestContext> options) : base(options)
    {
    }

    public DbSet<KafkaTopicState> KafkaTopicStates => this.Set<KafkaTopicState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddKafkaState();

        modelBuilder.Entity<TestEntityDb>().HasKey(x => x.Id);
        modelBuilder.Entity<TestEntityDb>().HasAlternateKey(x => x.ExternalId);
    }
}