// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Subscription;

#if EF6
using System.Data.Entity;
#else
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.Extensions.Logging;

#if EF6
namespace Epam.Kafka.PubSub.EntityFramework6.Subscription;
#else
namespace Epam.Kafka.PubSub.EntityFrameworkCore.Subscription;
#endif

/// <summary>
///     Base class to implement <see cref="ISubscriptionHandler{TKey,TValue}" /> that save entities of type
///     <typeparamref name="TEntity" /> created from kafka messages to database via DB Context.
/// </summary>
/// <typeparam name="TKey">The kafka message key type.</typeparam>
/// <typeparam name="TValue">The kafka message value type.</typeparam>
/// <typeparam name="TContext">The context type.</typeparam>
/// <typeparam name="TEntity">The entity type.</typeparam>
public abstract class
    DbContextEntitySubscriptionHandler<TKey, TValue, TContext, TEntity> : DbContextSubscriptionHandler<TKey, TValue,
        TContext>
    where TKey : notnull
    where TContext : DbContext
    where TEntity : class
{
    /// <inheritdoc />
    protected DbContextEntitySubscriptionHandler(TContext context, ILogger logger) : base(context, logger)
    {
        this.Set = this.Context.Set<TEntity>();
    }

    /// <summary>
    ///     The DB Set related to entities of type <typeparamref name="TContext" />.
    /// </summary>
    protected DbSet<TEntity> Set { get; }

    /// <inheritdoc />
    protected sealed override void LoadEntitiesChunk(IReadOnlyCollection<ConsumeResult<TKey, TValue>> chunk)
    {
        if (chunk == null)
        {
            throw new ArgumentNullException(nameof(chunk));
        }

        this.LoadMainChunk(this.Set.AsTracking(), chunk);

        this.LoadRelatedChunk(chunk);
    }

    /// <summary>
    ///     Invoked to load to context entities that should be created or updated from kafka messages.
    /// </summary>
    /// <param name="queryable">The query to load from.</param>
    /// <param name="chunk">The chunk of kafka messages.</param>
    protected abstract void LoadMainChunk(IQueryable<TEntity> queryable,
        IReadOnlyCollection<ConsumeResult<TKey, TValue>> chunk);

    /// <summary>
    ///     Invoked to load to context entities related to main entities.
    /// </summary>
    /// <param name="chunk">The chunk of kafka messages.</param>
    protected virtual void LoadRelatedChunk(IReadOnlyCollection<ConsumeResult<TKey, TValue>> chunk)
    {
    }

    /// <inheritdoc />
    protected sealed override object? FindLocal(ConsumeResult<TKey, TValue> value)
    {
        return this.FindLocal(this.Set, value);
    }

    /// <summary>
    ///     Invoked to find entity related to <paramref name="value" /> in <paramref name="dbSet" />.
    /// </summary>
    /// <param name="dbSet">The DB Set to load from.</param>
    /// <param name="value">Related kafka message representation.</param>
    /// <returns>Existing entity (preloaded by <see cref="LoadMainChunk" />) or null it it not exists.</returns>
    protected abstract TEntity? FindLocal(DbSet<TEntity> dbSet, ConsumeResult<TKey, TValue> value);

    /// <inheritdoc />
    protected sealed override string? Update(ConsumeResult<TKey, TValue> value, object entity, bool created)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        if (entity is TEntity typedEntity)
        {
            return this.Update(value, typedEntity, created);
        }

        throw new InvalidOperationException(
            $"Entity type expected: '{typeof(TEntity)}', actual:'{entity.GetType()}'.");
    }

    /// <summary>
    ///     Invoked to update <paramref name="entity" /> from <paramref name="value" />.
    /// </summary>
    /// <param name="value">The kafka message representation.</param>
    /// <param name="entity">The entity to update.</param>
    /// <param name="created">Whether entity was created as part of processing pipeline or already exists.</param>
    /// <returns>string representation of update result in free form. Will be used or logging purposes.</returns>
    protected abstract string? Update(ConsumeResult<TKey, TValue> value, TEntity entity, bool created);

    /// <inheritdoc />
    protected sealed override bool TryCreate(ConsumeResult<TKey, TValue> value, out object? entity)
    {
        bool result = this.TryCreate(value, out TEntity? e);
        entity = e;

        return result;
    }

    /// <summary>
    ///     Invoked to try to create <paramref name="entity" /> from <paramref name="value" />.
    /// </summary>
    /// <param name="value">The kafka message representation to created from.</param>
    /// <param name="entity">The newly created entity.</param>
    /// <returns>True if entity was created or false if creation was rejected.</returns>
    protected abstract bool TryCreate(ConsumeResult<TKey, TValue> value, out TEntity? entity);
}