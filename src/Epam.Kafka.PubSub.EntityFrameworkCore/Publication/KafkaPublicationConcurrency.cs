// Copyright © 2024 EPAM Systems

namespace Epam.Kafka.PubSub.EntityFrameworkCore.Publication;

/// <summary>
///     Behaviour configuration for concurrency exception on update of entity publication state.
/// </summary>
public enum KafkaPublicationConcurrency
{
    /// <summary>
    ///     Detach entity from context. Status update will be ignored only for item that cause exception.
    /// </summary>
    Detach = 0,

    /// <summary>
    ///     Re throw exception. Status update will be ignored for all items in batch.
    /// </summary>
    Throw = 1,

    /// <summary>
    ///     Re throw exception only if transactional producer is used. Status update will be ignored for all items in batch.
    /// </summary>
    ThrowIfTransaction = 2
}