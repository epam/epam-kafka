// Copyright © 2024 EPAM Systems

#if EF6
using System.Data.Entity;
#else
using Microsoft.EntityFrameworkCore;
#endif

#if EF6
namespace Epam.Kafka.PubSub.EntityFramework6.Subscription.State;
#else
namespace Epam.Kafka.PubSub.EntityFrameworkCore.Subscription.State;
#endif

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