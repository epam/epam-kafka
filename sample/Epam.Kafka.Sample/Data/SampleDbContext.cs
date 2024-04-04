// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.EntityFrameworkCore.Subscription.State;

using Microsoft.EntityFrameworkCore;

namespace Epam.Kafka.Sample.Data;

public class SampleDbContext : DbContext, IKafkaStateDbContext
{
    public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options)
    {
    }

    public DbSet<KafkaTopicState> KafkaTopicStates => this.Set<KafkaTopicState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddKafkaState();

        modelBuilder.Entity<SampleSubscriptionEntity>();
        modelBuilder.Entity<SamplePublicationEntity>();
    }
}