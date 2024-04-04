// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka.PubSub.Common.Pipeline;

namespace Epam.Kafka.PubSub.Subscription.Pipeline;

/// <summary>
///     The result of subscription batch iteration.
/// </summary>
public enum SubscriptionBatchResult
{
    /// <summary>
    ///     Result not available. Might be the case for first batch after pipeline (re)start.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Finished with errors.
    /// </summary>
    Error = BatchResult.Error,

    /// <summary>
    ///     Finished without consumed messages.
    /// </summary>
    Empty = BatchResult.Empty,

    /// <summary>
    ///     At least one message was successfully consumed and processed.
    /// </summary>
    Processed = BatchResult.Processed,

    /// <summary>
    ///     Topic partition for consume not assigned. Valid only for for internal and combined offsets storage.
    /// </summary>
    NotAssigned = 4,

    /// <summary>
    ///     All assigned topic partitions paused (pointing to <see cref="Offset.End"/> special value). Valid only for external and combined offsets storage.
    /// </summary>
    Paused = 5
}