// Copyright © 2024 EPAM Systems

using System.Data.Common;
using Epam.Kafka.PubSub.EntityFramework6.Subscription.State;

using System.Data.Entity;

namespace Epam.Kafka.Sample.Net462.Data
{
    public class SampleDbContext : DbContext, IKafkaStateDbContext
    {
        public SampleDbContext(DbConnection connection):base(connection,false)
        {
            
        }
        public DbSet<KafkaTopicState> KafkaTopicStates => this.Set<KafkaTopicState>();

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.AddKafkaState();

            modelBuilder.Entity<SampleSubscriptionEntity>();
            modelBuilder.Entity<SamplePublicationEntity>();
        }
    }
}