// Copyright © 2024 EPAM Systems

namespace Epam.Kafka.PubSub.Publication;

/// <summary>
///     Used by publication background service to get messages that should be published and manage their state.
/// </summary>
/// <typeparam name="TKey">The message key type.</typeparam>
/// <typeparam name="TValue">The message value type.</typeparam>
public interface IPublicationHandler<TKey, TValue>
{
    /// <summary>
    ///     Invoked when messages were committed to kafka in case of transactional producer is used, otherwise never invoked.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken" />.</param>
    void TransactionCommitted(CancellationToken cancellationToken);

    /// <summary>
    ///     Invoked to get messages that should be published.
    /// </summary>
    /// <param name="count">Max number of entities to return according to batch size configuration.</param>
    /// <param name="transaction">Whether transactional producer is used.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken" />.</param>
    IReadOnlyCollection<TopicMessage<TKey, TValue>> GetBatch(int count, bool transaction,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Callback method that invoked when delivery reports are available.
    /// </summary>
    /// <param name="reports">The delivery reports for messages.</param>
    /// <param name="transactionEnd">Transaction end time in case of transactional producer or null otherwise</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken" />.</param>
    void ReportResults(IDictionary<TopicMessage<TKey, TValue>, DeliveryReport> reports, DateTimeOffset? transactionEnd,
        CancellationToken cancellationToken);
}