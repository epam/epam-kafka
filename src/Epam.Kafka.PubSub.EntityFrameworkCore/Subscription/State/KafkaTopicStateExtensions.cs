// Copyright © 2024 EPAM Systems

#if EF6
namespace Epam.Kafka.PubSub.EntityFramework6.Subscription.State;
#else
namespace Epam.Kafka.PubSub.EntityFrameworkCore.Subscription.State;
#endif

#if EF6
using ModelBuilder = System.Data.Entity.DbModelBuilder;
using ETBuilder = System.Data.Entity.ModelConfiguration.EntityTypeConfiguration<KafkaTopicState>;
#else
using Microsoft.EntityFrameworkCore;
using ETBuilder = Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<KafkaTopicState>;
#endif

/// <summary>
///     Extension methods to configure an <see cref="ModelBuilder" /> for <see cref="KafkaTopicState" /> entity.
/// </summary>
public static class KafkaTopicStateExtensions
{
    /// <summary>
    ///     Add <see cref="KafkaTopicState" /> entity to context and performs it default configuration.
    /// </summary>
    /// <param name="builder">The <see cref="ModelBuilder" />.</param>
    /// <returns>The <see cref="ETBuilder" /> for additional configuration.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static ETBuilder AddKafkaState(this ModelBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        ETBuilder e = builder.Entity<KafkaTopicState>();

        e.HasKey(x => new { x.Topic, x.Partition, x.ConsumerGroup });

        e.Property(x => x.Topic).IsRequired().HasMaxLength(255).IsUnicode(false);
        e.Property(x => x.Partition).IsRequired();
        e.Property(x => x.ConsumerGroup).IsRequired().HasMaxLength(255).IsUnicode(false);

        e.Property(x => x.Offset).IsRequired().IsConcurrencyToken();
        e.Property(x => x.Pause).IsRequired();

        return e;
    }
}