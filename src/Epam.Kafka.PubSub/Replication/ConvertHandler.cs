// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Publication;

namespace Epam.Kafka.PubSub.Replication;

/// <inheritdoc />
public abstract class ConvertHandler<TKey, TValue, TEntity> : IConvertHandler<TKey, TValue, TEntity>
{
    /// <inheritdoc />
    public IReadOnlyCollection<TopicMessage<TKey, TValue>> Convert(IReadOnlyCollection<TEntity> entities)
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));

        List<TopicMessage<TKey, TValue>> result = new(entities.Count);

        result.AddRange(entities.SelectMany(this.ConvertSingle));

        return result;
    }

    /// <summary>
    /// Invoked by <see cref="Convert"/> method to convert single entity.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    protected abstract IEnumerable<TopicMessage<TKey, TValue>> ConvertSingle(TEntity entity);
}