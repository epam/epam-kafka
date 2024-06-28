// Copyright © 2024 EPAM Systems

#if EF6
namespace Epam.Kafka.PubSub.EntityFramework6.Publication.Contracts;
#else
namespace Epam.Kafka.PubSub.EntityFrameworkCore.Publication.Contracts;
#endif

/// <summary>
///     Default entity state that can be used to build publication pipeline. Not mandatory, any convenient way of state
///     organization may be used instead.
/// </summary>
public enum KafkaPublicationState
{
    /// <summary>
    ///     The entity should NOT be published.
    /// </summary>
    None = 0,

    /// <summary>
    ///     The entity can be published and is waiting for it.
    /// </summary>
    Queued = 1,

    /// <summary>
    ///     The entity published, however transaction not committed yet. Attempt to publish should be performed again after
    ///     transaction timeout expiration.
    /// </summary>
    Delivered = 2,

    /// <summary>
    ///     The entity published and transaction either not used or successfully committed.
    /// </summary>
    Committed = 3,

    /// <summary>
    ///     The entity not published due to error. Attempt to publish should be performed again after some timeout.
    /// </summary>
    Error = 4
}