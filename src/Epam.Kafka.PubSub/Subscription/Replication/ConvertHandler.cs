// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Publication;

namespace Epam.Kafka.PubSub.Subscription.Replication;

/// <inheritdoc />
public abstract class ConvertHandler<TKey, TValue, TEntity> : IConvertHandler<TKey, TValue, TEntity>
{
    /// <inheritdoc />
    public IReadOnlyCollection<TopicMessage<TKey, TValue>> Convert(IReadOnlyCollection<TEntity> entities, CancellationToken cancellationToken)
    {
        if (entities == null) throw new ArgumentNullException(nameof(entities));

        List<TopicMessage<TKey, TValue>> result = new(entities.Count);

        result.AddRange(entities.SelectMany(x => this.ConvertSingle(x, cancellationToken)));

        return result;
    }

    /// <summary>
    /// Invoked by <see cref="Convert"/> method to convert single entity.
    /// </summary>
    /// <param name="entity">The entity from which topic messages should be produced</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns></returns>
    protected abstract IEnumerable<TopicMessage<TKey, TValue>> ConvertSingle(TEntity entity, CancellationToken cancellationToken);
}