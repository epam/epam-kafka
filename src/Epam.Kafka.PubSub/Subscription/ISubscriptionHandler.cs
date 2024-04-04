// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

namespace Epam.Kafka.PubSub.Subscription;

/// <summary>
///     Used by subscription background service to process consumed messages batch.
/// </summary>
/// <typeparam name="TKey">The message key type.</typeparam>
/// <typeparam name="TValue">The message value type.</typeparam>
public interface ISubscriptionHandler<TKey, TValue>
{
    /// <summary>
    ///     Invoked to process consumer messages batch.
    /// </summary>
    /// <param name="items">The kafka messages to process.</param>
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> representing application shut down or processing
    ///     timeout.
    /// </param>
    void Execute(IReadOnlyCollection<ConsumeResult<TKey, TValue>> items, CancellationToken cancellationToken);
}