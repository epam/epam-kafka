// Copyright © 2024 EPAM Systems

namespace Epam.Kafka.PubSub.Publication;

/// <summary>
/// Handler to convert entity for one or multiple topic messages
/// </summary>
/// <typeparam name="TKey">The message key type.</typeparam>
/// <typeparam name="TValue">The message value type.</typeparam>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IConvertHandler<TKey, TValue, in TEntity>
    where TEntity : notnull
{
    /// <summary>
    ///     Create single or multiple messages to produce from entity
    /// </summary>
    /// <param name="entity">The entity from which messages should be constructed.</param>
    /// <returns></returns>
    IEnumerable<TopicMessage<TKey, TValue>> Convert(TEntity entity);
}