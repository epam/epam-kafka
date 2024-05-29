// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Publication;

#if EF6
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
#else
using Microsoft.EntityFrameworkCore;
#endif
using Microsoft.Extensions.Logging;

using System.Linq.Expressions;

#if EF6
namespace Epam.Kafka.PubSub.EntityFramework6.Publication.Contracts;
#else
namespace Epam.Kafka.PubSub.EntityFrameworkCore.Publication.Contracts;
#endif

/// <summary>
///     Base class to implement publication source that publish entities that implement
///     <see cref="IKafkaPublicationEntity" /> from database via DB Context to kafka topics.
/// </summary>
/// <typeparam name="TKey">The kafka message key type.</typeparam>
/// <typeparam name="TValue">The kafka message value type.</typeparam>
/// <typeparam name="TEntity">The db entity type that implements <see cref="IKafkaPublicationEntity" />.</typeparam>
/// <typeparam name="TContext">The context type.</typeparam>
public abstract class
    DbContextEntityPublicationHandler<TKey, TValue, TEntity, TContext> : DbContextPublicationHandler<TKey, TValue,
        TEntity, TContext>
    where TEntity : class, IKafkaPublicationEntity
    where TContext : DbContext
{
    /// <inheritdoc />
    protected DbContextEntityPublicationHandler(TContext context, ILogger logger) : base(context, logger)
    {
    }

    /// <summary>
    ///     Timeout for entity processing in case of error. Will be used to calculate
    ///     <see cref="IKafkaPublicationEntity.KafkaPubNbf" />. Default 5 minutes.
    /// </summary>
    protected TimeSpan ErrorRetryTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    protected override Expression<Func<TEntity, bool>> IsQueued { get; } = x =>
        x.KafkaPubState == KafkaPublicationState.Queued
        || (x.KafkaPubState == KafkaPublicationState.Error && x.KafkaPubNbf <= DateTime.UtcNow)
        || (x.KafkaPubState == KafkaPublicationState.Delivered && x.KafkaPubNbf <= DateTime.UtcNow);

    /// <inheritdoc />
    protected override void TransactionCommitted(IReadOnlyCollection<TEntity> entities,
        CancellationToken cancellationToken)
    {
        if (entities == null)
        {
            throw new ArgumentNullException(nameof(entities));
        }

        foreach (TEntity entity in entities.Where(x => x.KafkaPubState == KafkaPublicationState.Delivered))
        {
            entity.KafkaPubState = KafkaPublicationState.Committed;
        }

        try
        {
            this.Context.SaveChanges(true);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            foreach (var entry in exception.Entries)
            {
                this.Logger.PublicationEntityDetached(exception, "Commit", this.FindPrimaryKeyForLogs(entry), typeof(TEntity));
            }
        }
    }

    /// <inheritdoc />
    protected override bool ConvertErrorHandled(TEntity entity, bool transaction, Exception exception)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        if (exception == null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        entity.KafkaPubState = KafkaPublicationState.Error;
        entity.KafkaPubNbf = DateTime.UtcNow + this.ErrorRetryTimeout;

        this.Context.SaveChanges();

        return true;
    }

    /// <inheritdoc />
    protected sealed override void Callback(TEntity entity, IReadOnlyCollection<DeliveryReport> reports,
        DateTimeOffset? transactionEnd)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        if (reports == null)
        {
            throw new ArgumentNullException(nameof(reports));
        }

        DeliveryReport? error = reports.FirstOrDefault(x => x.Error.IsError);

        if (error != null)
        {
            this.ErrorCallback(entity, error);
        }
        else
        {
            this.SuccessCallback(entity, reports, transactionEnd);
        }
    }

    /// <summary>
    ///     Callback method that invoked when at least one delivery report has error.
    /// </summary>
    /// <param name="entity">The published entity.</param>
    /// <param name="report">The first <see cref="DeliveryReport" /> with error.</param>
    /// <exception cref="ArgumentNullException"></exception>
    protected virtual void ErrorCallback(TEntity entity, DeliveryReport report)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        entity.KafkaPubState = KafkaPublicationState.Error;
        entity.KafkaPubNbf = DateTime.UtcNow + this.ErrorRetryTimeout;
    }

    /// <summary>
    ///     Callback method that invoked when all delivery reports without errors.
    /// </summary>
    /// <param name="entity">The published entity.</param>
    /// <param name="reports">The delivery reports from messages created from entity.</param>
    /// <param name="transactionEnd">
    ///     Transaction timeout expiration in case transactional producer is used, otherwise
    ///     <value>null</value>
    /// </param>
    /// <exception cref="ArgumentNullException"></exception>
    protected virtual void SuccessCallback(TEntity entity, IReadOnlyCollection<DeliveryReport> reports,
        DateTimeOffset? transactionEnd)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        if (transactionEnd.HasValue)
        {
            entity.KafkaPubState = KafkaPublicationState.Delivered;
            entity.KafkaPubNbf = transactionEnd.Value.UtcDateTime;
        }
        else
        {
            entity.KafkaPubState = KafkaPublicationState.Committed;
        }
    }
}