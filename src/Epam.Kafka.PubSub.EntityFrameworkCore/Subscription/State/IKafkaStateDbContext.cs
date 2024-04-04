// Copyright © 2024 EPAM Systems

using Microsoft.EntityFrameworkCore;

namespace Epam.Kafka.PubSub.EntityFrameworkCore.Subscription.State;

/// <summary>
///     Interface indicating that DBContext holds information about <see cref="KafkaTopicState" />.
/// </summary>
public interface IKafkaStateDbContext
{
    /// <summary>
    ///     The <see cref="DbSet{KafkaTopicState}" /> to CRUD operations with subscription state.
    /// </summary>
    DbSet<KafkaTopicState> KafkaTopicStates { get; }
}