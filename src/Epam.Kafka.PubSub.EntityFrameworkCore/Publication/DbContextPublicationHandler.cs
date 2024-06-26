// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Publication;

#if EF6
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

using TEntry = System.Data.Entity.Infrastructure.DbEntityEntry;

#else
using Microsoft.EntityFrameworkCore;

using TEntry = Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry;

#endif
using Microsoft.Extensions.Logging;

using System.Linq.Expressions;

#if EF6
namespace Epam.Kafka.PubSub.EntityFramework6.Publication;
#else
namespace Epam.Kafka.PubSub.EntityFrameworkCore.Publication;
#endif

/// <summary>
///     Base class to implement publication source that publish entities from database via DB Context to kafka topics.
/// </summary>
/// <typeparam name="TKey">The kafka message key type.</typeparam>
/// <typeparam name="TValue">The kafka message value type.</typeparam>
/// <typeparam name="TEntity">The db entity type.</typeparam>
/// <typeparam name="TContext">The context type.</typeparam>
public abstract class
    DbContextPublicationHandler<TKey, TValue, TEntity, TContext> : PublicationHandler<TKey, TValue, TEntity>
    where TEntity : class
    where TContext : DbContext
{
    /// <inheritdoc />
    protected DbContextPublicationHandler(TContext context, ILogger logger) : base(logger)
    {
        this.Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    ///     The DB context used to access entity of type <typeparamref name="TEntity" />.
    /// </summary>
    protected TContext Context { get; }

    /// <summary>
    ///     Filter expression to define entities that are in state that allows publication.
    /// </summary>
    protected abstract Expression<Func<TEntity, bool>> IsQueued { get; }

    /// <summary>
    ///     The <see cref="KafkaPublicationConcurrency" />. Default <see cref="KafkaPublicationConcurrency.Detach" />.
    /// </summary>
    protected virtual KafkaPublicationConcurrency OnConcurrencyException => KafkaPublicationConcurrency.Detach;

    /// <inheritdoc />
    protected override IEnumerable<TEntity> GetEntities(int count, bool transaction,
        CancellationToken cancellationToken)
    {
        return this.OrderBy(this.GetTable().AsTracking().Where(this.IsQueued));
    }

    /// <summary>
    ///     Method to get query for selecting entities that should be published.
    /// </summary>
    /// <returns>The <see cref="IQueryable{TEntity}" />.</returns>
    protected virtual IQueryable<TEntity> GetTable()
    {
        return this.Context.Set<TEntity>();
    }

    /// <summary>
    ///     Method to define entities publication order.
    /// </summary>
    /// <param name="query">The <see cref="IQueryable{TEntity}" />.</param>
    /// <returns>The <see cref="IQueryable{TEntity}" />.</returns>
    protected abstract IOrderedQueryable<TEntity> OrderBy(IQueryable<TEntity> query);

    /// <inheritdoc />
    protected sealed override void Callback(IReadOnlyDictionary<TEntity, IReadOnlyCollection<DeliveryReport>> reports,
        DateTimeOffset? transactionEnd, CancellationToken cancellationToken)
    {
        if (reports == null)
        {
            throw new ArgumentNullException(nameof(reports));
        }

        foreach (KeyValuePair<TEntity, IReadOnlyCollection<DeliveryReport>> report in reports)
        {
            this.Callback(report.Key, report.Value, transactionEnd);
        }

        try
        {
            this.Context.SaveChanges(true);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            if (this.OnConcurrencyException == KafkaPublicationConcurrency.Throw
                || (this.OnConcurrencyException == KafkaPublicationConcurrency.ThrowIfTransaction &&
                    transactionEnd.HasValue))
            {
                throw;
            }

            foreach (TEntry? entry in exception.Entries)
            {
                entry.State = EntityState.Detached;

                this.Logger.PublicationEntityDetached(exception, "Report", this.FindPrimaryKeyForLogs(entry), typeof(TEntity));
            }

            this.Context.SaveChanges(true);
        }
    }

    /// <summary>
    /// Find entry primary key for logging.
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    protected virtual object? FindPrimaryKeyForLogs(TEntry entry)
    {
        if (entry == null) return null;

#pragma warning disable CA1031 // Don't fail in case of issue with logging.
        try
        {
#if EF6
            return null; // Unable to get it for EF6
#else
            return entry.Metadata.FindPrimaryKey()?.Properties.Select(x => entry.CurrentValues[x]).ToArray().FirstOrDefault();
#endif
        }
        // 
        catch
        {
            return null;
        }
#pragma warning restore CA1031
    }

    /// <summary>
    ///     The callback method that invoked when delivery reports for messages produced from entity are available. Use it to
    ///     update entity state checked by <see cref="IsQueued" /> expression to prevent infinite publication.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <param name="reports">The delivery reports for messages produced from entity.</param>
    /// <param name="transactionEnd">
    ///     Transaction timeout expiration in case transactional producer is used, otherwise
    ///     <value>null</value>
    /// </param>
    protected abstract void Callback(TEntity entity, IReadOnlyCollection<DeliveryReport> reports,
        DateTimeOffset? transactionEnd);
}