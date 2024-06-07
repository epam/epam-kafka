// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Subscription;

#if EF6
using System.Data.Entity;
#else
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.Extensions.Logging;

using System.Diagnostics;

#if EF6
namespace Epam.Kafka.PubSub.EntityFramework6.Subscription;
#else
namespace Epam.Kafka.PubSub.EntityFrameworkCore.Subscription;
#endif

/// <summary>
///     Base class to implement <see cref="ISubscriptionHandler{TKey,TValue}" /> that save entities created from kafka
///     messages to database via DB Context.
/// </summary>
/// <typeparam name="TKey">The kafka message key type.</typeparam>
/// <typeparam name="TValue">The kafka message value type.</typeparam>
/// <typeparam name="TContext">The context type.</typeparam>
public abstract class DbContextSubscriptionHandler<TKey, TValue, TContext> : SubscriptionHandler<TKey, TValue>
    where TKey : notnull
    where TContext : DbContext
{
    private int _chunkSize = 1000;

    /// <inheritdoc />
    protected DbContextSubscriptionHandler(TContext context, ILogger logger) : base(logger)
    {
        this.Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    ///     The DB Context used to CRUD entities.
    /// </summary>
    protected TContext Context { get; }

    /// <summary>
    ///     The chunk size used by <see cref="LoadEntitiesChunk" /> method to not exceed max number of logical operators in SQL
    ///     query. Default <c>1000</c>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    protected int ChunkSize
    {
        get => this._chunkSize;
        set => this._chunkSize = value > 0
            ? value
            : throw new ArgumentOutOfRangeException(nameof(this.ChunkSize), "Chunk size should be greater than 0");
    }

    /// <summary>
    ///     Invoked to load to context entities that should be created or updated from kafka messages.
    /// </summary>
    /// <param name="chunk">The chunk of kafka messages.</param>
    protected abstract void LoadEntitiesChunk(IReadOnlyCollection<ConsumeResult<TKey, TValue>> chunk);

    /// <summary>
    ///     Invoked to find entity related to <paramref name="value" /> in DB context.
    /// </summary>
    /// <param name="value">Related kafka message representation.</param>
    /// <returns>Existing entity (preloaded by <see cref="LoadEntitiesChunk" />) or null it it not exists.</returns>
    protected abstract object? FindLocal(ConsumeResult<TKey, TValue> value);

    /// <summary>
    ///     Invoked to try to create <paramref name="entity" /> from <paramref name="value" />.
    /// </summary>
    /// <param name="value">The kafka message representation to created from.</param>
    /// <param name="entity">The newly created entity.</param>
    /// <returns>True if entity was created or false if creation was rejected.</returns>
    protected abstract bool TryCreate(ConsumeResult<TKey, TValue> value, out object? entity);

    /// <summary>
    ///     Invoked to check if related entity should be deleted (if exists) instead of creation or update.
    /// </summary>
    /// <param name="value">Whether related entity should be deleted (if exists) instead of creation or update</param>
    /// <returns></returns>
    protected abstract bool IsDeleted(ConsumeResult<TKey, TValue> value);

    /// <summary>
    ///     Invoked to update <paramref name="entity" /> from <paramref name="value" />.
    /// </summary>
    /// <param name="value">The kafka message representation.</param>
    /// <param name="entity">The entity to update.</param>
    /// <param name="created">Whether entity was created as part of processing pipeline or already exists.</param>
    /// <returns>string representation of update result in free form. Will be used or logging purposes.</returns>
    protected abstract string? Update(ConsumeResult<TKey, TValue> value, object entity, bool created);

    /// <inheritdoc />
    protected override void ProcessBatch(IDictionary<ConsumeResult<TKey, TValue>, string?> items,
        CancellationToken cancellationToken)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        var batch = new List<ConsumeResult<TKey, TValue>>(this.ChunkSize);

        using (var a = new Activity("LoadChunks"))
        {
            a.Start();

            for (int index = 0; ; index += this.ChunkSize)
            {
                batch.Clear();
                batch.AddRange(items.Where(x => x.Value == null).Skip(index).Take(this.ChunkSize).Select(x => x.Key));

                if (batch.Count > 0)
                {
                    this.LoadEntitiesChunk(batch);
                }
                else
                {
                    break;
                }
            }

            a.Stop();
        }

        base.ProcessBatch(items, cancellationToken);

        using (var a = new Activity("SaveChanges"))
        {
            a.Start();

            this.SaveChanges();

            a.Stop();
        }
    }

    /// <summary>
    ///     Invoked at the end of <see cref="ProcessBatch" />. Default implementation calls
    ///     <see cref="DbContext.SaveChanges()" />
    /// </summary>
    protected virtual void SaveChanges()
    {
        this.Context.SaveChanges(true);
    }

    /// <inheritdoc />
    protected override string ProcessSingle(ConsumeResult<TKey, TValue> item)
    {
        object? entity = this.FindLocal(item);

        if (entity == null)
        {
            if (this.TryCreate(item, out entity))
            {
                this.Context.Add(entity!);

                this.Update(item, entity!, true);

                return "Created";
            }

            return "NotCreated";
        }

        if (this.IsDeleted(item))
        {
            this.Context.Remove(entity);
            return "Deleted";
        }

        return this.Update(item, entity, false) ?? "Updated";
    }
}