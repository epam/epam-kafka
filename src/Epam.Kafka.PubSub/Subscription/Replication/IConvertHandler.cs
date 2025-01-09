// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Publication;

namespace Epam.Kafka.PubSub.Subscription.Replication;

/// <summary>
/// The handler to convert entities to topic messages.
/// </summary>
/// <typeparam name="TKey">The message key type</typeparam>
/// <typeparam name="TValue">The message value type</typeparam>
/// <typeparam name="TEntity">The entity type</typeparam>
public interface IConvertHandler<TKey, TValue, in TEntity>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entities">The entities from which topic messages should be produced</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns></returns>
    IReadOnlyCollection<TopicMessage<TKey, TValue>> Convert(IReadOnlyCollection<TEntity> entities, CancellationToken cancellationToken);
}