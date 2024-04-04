// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Utils;

using Microsoft.Extensions.Logging;

namespace Epam.Kafka.PubSub.Publication;

/// <summary>
///     Base class to implement <see cref="IPublicationHandler{TKey,TValue}" /> that publish multiple messages created from
///     single entity.
/// </summary>
/// <typeparam name="TKey">The message key type.</typeparam>
/// <typeparam name="TValue">The message value type.</typeparam>
/// <typeparam name="TEntity">The entity type.</typeparam>
public abstract class PublicationHandler<TKey, TValue, TEntity> : IPublicationHandler<TKey, TValue>
    where TEntity : notnull
{
    private readonly Dictionary<TopicMessage<TKey, TValue>, TEntity> _batch = new();

    /// <summary>
    ///     Initialize new instance of <see cref="PublicationHandler{TKey,TValue,TEntity}" />
    /// </summary>
    /// <param name="logger">The <see cref="ILogger" />.</param>
    /// <exception cref="ArgumentNullException"></exception>
    protected PublicationHandler(ILogger logger)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     The <see cref="ILogger" />.
    /// </summary>
    protected ILogger Logger { get; }

    /// <inheritdoc />
    public void TransactionCommitted(CancellationToken cancellationToken)
    {
        if (this._batch.Count == 0)
        {
            throw new InvalidOperationException("Not allowed to commit transaction for empty batch");
        }

        this.TransactionCommitted(this._batch.Values, cancellationToken);
    }

    /// <inheritdoc />
    public IReadOnlyCollection<TopicMessage<TKey, TValue>> GetBatch(int count, bool transaction,
        CancellationToken cancellationToken)
    {
        if (this._batch.Count > 0)
        {
            throw new InvalidOperationException("Not allowed to execute GetBatch more than 1 time");
        }

        IEnumerable<TEntity> items = this.GetEntities(count, transaction, cancellationToken);

        // buffer to trigger convert result enumeration
        List<TopicMessage<TKey, TValue>> entityToMessage = new();

        foreach (TEntity entity in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            entityToMessage.Clear();

            try
            {
                entityToMessage.AddRange(this.Convert(entity));
            }
            catch (Exception exception)
            {
                if (!this.ConvertErrorHandled(entity, transaction, exception))
                {
                    throw;
                }

                this.Logger.ConvertError(exception, typeof(TEntity));
            }

            foreach (TopicMessage<TKey, TValue> message in entityToMessage)
            {
                this._batch.Add(message, entity);
            }
        }

        return this._batch.Keys;
    }

    /// <inheritdoc />
    public void ReportResults(IDictionary<TopicMessage<TKey, TValue>, DeliveryReport> reports,
        DateTimeOffset? transactionEnd, CancellationToken cancellationToken)
    {
        if (reports == null)
        {
            throw new ArgumentNullException(nameof(reports));
        }

        if (this._batch.Count == 0)
        {
            throw new InvalidOperationException("Not allowed to report results for empty batch");
        }

        IEnumerable<IGrouping<TEntity, KeyValuePair<TopicMessage<TKey, TValue>, DeliveryReport>>> groupBy =
            reports.GroupBy(x => this._batch[x.Key]);

        var dictionary = groupBy.ToDictionary(p => p.Key,
            p => p.Select(x => x.Value).ToArray() as IReadOnlyCollection<DeliveryReport>);

        this.Callback(dictionary, transactionEnd, cancellationToken);
    }

    /// <inheritdoc cref="IPublicationHandler{TKey,TValue}.TransactionCommitted" />
    /// <param name="entities">Entities that published in current batch.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken" />.</param>
    protected abstract void TransactionCommitted(IReadOnlyCollection<TEntity> entities,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Invoked to get entities that should be published.
    /// </summary>
    /// <param name="count">Max number of entities to return according to batch size configuration.</param>
    /// <param name="transaction">Whether transactional producer is used.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken" />.</param>
    /// <returns></returns>
    protected abstract IEnumerable<TEntity> GetEntities(int count, bool transaction,
        CancellationToken cancellationToken);

    /// <summary>
    ///     Invoked in case of exception in <see cref="Convert" /> method.
    ///     By default return false to trigger exception re-throw and terminate batch processing.
    ///     It is possible to override and return true. In this case batch processing will be continued without entity which
    ///     caused exception.
    /// </summary>
    /// <param name="entity">Entity that caused exception</param>
    /// <param name="transaction">Whether the publisher use KAFKA transactions or not.</param>
    /// <param name="exception">Occurred exception</param>
    /// <returns></returns>
    protected virtual bool ConvertErrorHandled(TEntity entity, bool transaction, Exception exception)
    {
        return false;
    }

    /// <summary>
    ///     Create single or multiple messages to produce from entity
    /// </summary>
    /// <param name="entity">The entity from which messages should be constructed.</param>
    /// <returns></returns>
    protected abstract IEnumerable<TopicMessage<TKey, TValue>> Convert(TEntity entity);

    /// <summary>
    ///     Callback method that invoked when delivery reports are available.
    /// </summary>
    /// <param name="reports">The delivery reports for entities.</param>
    /// <param name="transactionEnd">transaction end time in case of transactional producer or null otherwise</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken" />.</param>
    protected abstract void Callback(IReadOnlyDictionary<TEntity, IReadOnlyCollection<DeliveryReport>> reports,
        DateTimeOffset? transactionEnd, CancellationToken cancellationToken);
}