// Copyright © 2024 EPAM Systems

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Epam.Kafka.PubSub.EntityFrameworkCore.Subscription.State;

/// <summary>
///     Extension methods to configure an <see cref="ModelBuilder" /> for <see cref="KafkaTopicState" /> entity.
/// </summary>
public static class KafkaTopicStateExtensions
{
    /// <summary>
    ///     Add <see cref="KafkaTopicState" /> entity to context and performs it default configuration.
    /// </summary>
    /// <param name="builder">The <see cref="ModelBuilder" />.</param>
    /// <returns>The <see cref="EntityTypeBuilder{KafkaTopicState}" /> for additional configuration.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static EntityTypeBuilder<KafkaTopicState> AddKafkaState(this ModelBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        EntityTypeBuilder<KafkaTopicState> e = builder.Entity<KafkaTopicState>();

        e.HasKey(x => new { x.Topic, x.Partition, x.ConsumerGroup });

        e.Property(x => x.Topic).IsRequired().HasMaxLength(255).IsUnicode(false);
        e.Property(x => x.Partition).IsRequired();
        e.Property(x => x.ConsumerGroup).IsRequired().HasMaxLength(255).IsUnicode(false);

        e.Property(x => x.Offset).IsRequired().IsConcurrencyToken();
        e.Property(x => x.Pause).IsRequired();

        return e;
    }
}