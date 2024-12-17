// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Publication;

namespace Epam.Kafka.PubSub.Replication;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
/// <typeparam name="TEntity"></typeparam>
public interface IConvertHandler<TKey, TValue, in TEntity>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns></returns>
    IReadOnlyCollection<TopicMessage<TKey, TValue>> Convert(IReadOnlyCollection<TEntity> entities, CancellationToken cancellationToken);
}