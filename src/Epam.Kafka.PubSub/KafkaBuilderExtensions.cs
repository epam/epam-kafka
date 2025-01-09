// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Confluent.SchemaRegistry;

using Epam.Kafka.PubSub.Common;
using Epam.Kafka.PubSub.Common.HealthChecks;
using Epam.Kafka.PubSub.Publication;
using Epam.Kafka.PubSub.Publication.Options;
using Epam.Kafka.PubSub.Publication.Pipeline;
using Epam.Kafka.PubSub.Replication;
using Epam.Kafka.PubSub.Subscription;
using Epam.Kafka.PubSub.Subscription.Options;
using Epam.Kafka.PubSub.Subscription.Pipeline;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Epam.Kafka.PubSub;

/// <summary>
///     Extension methods to configure an <see cref="IServiceCollection" /> for subscription and publication background
///     services.
/// </summary>
public static class KafkaBuilderExtensions
{
    /// <summary>
    ///     Add background service that will consume messages from kafka and invoke
    ///     <see cref="ISubscriptionHandler{TKey,TValue}.Execute" /> to process them.
    /// </summary>
    /// <param name="builder">The <see cref="KafkaBuilder" />.</param>
    /// <param name="name">The name to identify subscription.</param>
    /// <param name="handlerLifetime">
    ///     The <see cref="ServiceLifetime" /> for <see cref="ISubscriptionHandler{TKey,TValue}" />
    ///     implementation. Default <c><see cref="ServiceLifetime.Transient"/></c>
    /// </param>
    /// <typeparam name="TKey">The message key type.</typeparam>
    /// <typeparam name="TValue">The message value type.</typeparam>
    /// <typeparam name="THandler">The <see cref="ISubscriptionHandler{TKey,TValue}" /> implementation type.</typeparam>
    /// <returns>The <see cref="SubscriptionBuilder{TKey,TValue}" /> to configure subscription behaviour.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static SubscriptionBuilder<TKey, TValue> AddSubscription<TKey, TValue, THandler>(
        this KafkaBuilder builder,
        string name,
        ServiceLifetime handlerLifetime = ServiceLifetime.Transient)
        where THandler : ISubscriptionHandler<TKey, TValue>
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        builder.GetOrCreateContext().AddSubscription(name);

        Type handlerType = typeof(THandler);

        TryRegisterHandler(builder.Services, handlerType, handlerLifetime);

        return new SubscriptionBuilder<TKey, TValue>(builder, name, handlerType);
    }

    /// <summary>
    ///     Add background service that will produce message to kafka using <see cref="IPublicationHandler{TKey,TValue}" /> to
    ///     get messages and manage their state.
    /// </summary>
    /// <param name="builder">The <see cref="KafkaBuilder" />.</param>
    /// <param name="name">The name to identify publication.</param>
    /// <param name="handlerLifetime">
    ///     The <see cref="ServiceLifetime" /> for <see cref="IPublicationHandler{TKey,TValue}" />
    ///     implementation. Default <c><see cref="ServiceLifetime.Transient"/></c>
    /// </param>
    /// <typeparam name="TKey">The message key type.</typeparam>
    /// <typeparam name="TValue">The message value type.</typeparam>
    /// <typeparam name="THandler">The <see cref="IPublicationHandler{TKey,TValue}" /> implementation type.</typeparam>
    /// <returns>The <see cref="PublicationBuilder{TKey,TValue}" /> to configure publication behaviour.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static PublicationBuilder<TKey, TValue> AddPublication<TKey, TValue, THandler>(
        this KafkaBuilder builder,
        string name,
        ServiceLifetime handlerLifetime = ServiceLifetime.Transient)
        where THandler : IPublicationHandler<TKey, TValue>
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        builder.GetOrCreateContext().AddPublication(name);

        Type handlerType = typeof(THandler);

        TryRegisterHandler(builder.Services, handlerType, handlerLifetime);

        return new PublicationBuilder<TKey, TValue>(builder, name, handlerType);
    }

    /// <summary>
    ///     Add subscription that will consume messages from kafka, then invoke
    ///     <see cref="IConvertHandler{TKey,TValue,TEntity}.Convert" /> to convert them,
    ///    and finally publish result of conversion to kafka.
    /// </summary>
    /// <param name="builder">The <see cref="KafkaBuilder" />.</param>
    /// <param name="name">The name to identify subscription.</param>
    /// <param name="handlerLifetime">
    ///     The <see cref="ServiceLifetime" /> for <see cref="IConvertHandler{TKey,TValue,TEntity}" />
    ///     implementation. Default <c><see cref="ServiceLifetime.Transient"/></c>
    /// </param>
    /// <param name="keySerializer">The output message key serializer factory</param>
    /// <param name="valueSerializer">The output message value serializer factory</param>
    /// <param name="partitioner">The output message partitioner configuration</param>
    /// <typeparam name="TSubKey">The input message key type.</typeparam>
    /// <typeparam name="TSubValue">The input message value type.</typeparam>
    /// <typeparam name="TPubKey">The output message key type.</typeparam>
    /// <typeparam name="TPubValue">The output message value type.</typeparam>
    /// <typeparam name="THandler">The <see cref="IConvertHandler{TKey,TValue,TEntity}" /> implementation type.</typeparam>
    /// <returns><inheritdoc cref="AddSubscription{TKey,TValue,THandler}"/></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static SubscriptionBuilder<TSubKey, TSubValue> AddReplication<TSubKey, TSubValue, TPubKey, TPubValue, THandler>(
        this KafkaBuilder builder,
        string name,
        ServiceLifetime handlerLifetime = ServiceLifetime.Transient,
        Func<Lazy<ISchemaRegistryClient>, ISerializer<TPubKey>>? keySerializer = null,
        Func<Lazy<ISchemaRegistryClient>, ISerializer<TPubValue>>? valueSerializer = null,
        Action<ProducerPartitioner>? partitioner = null)
        where THandler : IConvertHandler<TPubKey, TPubValue, ConsumeResult<TSubKey, TSubValue>>
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        builder.GetOrCreateContext().AddReplication(name);

        Type handlerType = typeof(THandler);

        TryRegisterHandler(builder.Services, handlerType, handlerLifetime);

        return new ReplicationBuilder<TSubKey, TSubValue, TPubKey, TPubValue>(builder, name)
            .WithOptions(x =>
            {
                x.Replication.ConvertHandlerType = handlerType;
                x.Replication.KeyType = typeof(TPubKey);
                x.Replication.ValueType = typeof(TPubValue);
                x.Replication.KeySerializer = keySerializer;
                x.Replication.ValueSerializer = valueSerializer;
                partitioner?.Invoke(x.Replication.Partitioner);
            });
    }

    /// <summary>
    ///     Register health check that verify all subscriptions and publications and returns:
    ///     <list type="string"><see cref="HealthStatus.Healthy" /> if ALL subscriptions and publications in Healthy state.</list>
    ///     <list type="string"><see cref="HealthStatus.Unhealthy" /> if ALL subscriptions and publications in Unhealthy state.</list>
    ///     <list type="string"><see cref="HealthStatus.Degraded" /> otherwise.</list>
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder" />.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <param name="failureStatus">
    ///     The <see cref="HealthStatus" /> that should be reported when the health check reports a failure. If the provided
    ///     value
    ///     is <c>null</c>, then <see cref="HealthStatus.Unhealthy" /> will be reported.
    /// </param>
    /// <remarks>
    ///     This method will use
    ///     <see
    ///         cref="HealthChecksBuilderAddCheckExtensions.AddCheck{T}(IHealthChecksBuilder,string,HealthStatus?,IEnumerable{string})" />
    ///     to register the health check.
    /// </remarks>
    /// <returns>The <see cref="KafkaBuilder" />.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static KafkaBuilder WithPubSubSummaryHealthCheck(this KafkaBuilder builder,
        IEnumerable<string>? tags = null,
        HealthStatus? failureStatus = null)
    {
        return builder.WithSummaryHealthCheck(PubSubSummaryHealthCheck.Name, tags, failureStatus);
    }

    /// <summary>
    ///     Register health check that verify all publications and returns:
    ///     <list type="string"><see cref="HealthStatus.Healthy" /> if ALL publications in Healthy state.</list>
    ///     <list type="string"><see cref="HealthStatus.Unhealthy" /> if ALL publications in Unhealthy state.</list>
    ///     <list type="string"><see cref="HealthStatus.Degraded" /> otherwise.</list>
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder" />.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <param name="failureStatus">
    ///     The <see cref="HealthStatus" /> that should be reported when the health check reports a failure. If the provided
    ///     value
    ///     is <c>null</c>, then <see cref="HealthStatus.Unhealthy" /> will be reported.
    /// </param>
    /// <remarks>
    ///     This method will use
    ///     <see
    ///         cref="HealthChecksBuilderAddCheckExtensions.AddCheck{T}(IHealthChecksBuilder,string,HealthStatus?,IEnumerable{string})" />
    ///     to register the health check.
    /// </remarks>
    /// <returns>The <see cref="KafkaBuilder" />.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static KafkaBuilder WithPublicationSummaryHealthCheck(this KafkaBuilder builder,
        IEnumerable<string>? tags = null,
        HealthStatus? failureStatus = null)
    {
        return builder.WithSummaryHealthCheck(PublicationMonitor.Prefix, tags, failureStatus);
    }

    /// <summary>
    ///     Register health check that verify all subscriptions and returns:
    ///     <list type="string"><see cref="HealthStatus.Healthy" /> if ALL subscriptions in Healthy state.</list>
    ///     <list type="string"><see cref="HealthStatus.Unhealthy" /> if ALL subscriptions in Unhealthy state.</list>
    ///     <list type="string"><see cref="HealthStatus.Degraded" /> otherwise.</list>
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder" />.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <param name="failureStatus">
    ///     The <see cref="HealthStatus" /> that should be reported when the health check reports a failure. If the provided
    ///     value
    ///     is <c>null</c>, then <see cref="HealthStatus.Unhealthy" /> will be reported.
    /// </param>
    /// <remarks>
    ///     This method will use
    ///     <see
    ///         cref="HealthChecksBuilderAddCheckExtensions.AddCheck{T}(IHealthChecksBuilder,string,HealthStatus?,IEnumerable{string})" />
    ///     to register the health check.
    /// </remarks>
    /// <returns>The <see cref="KafkaBuilder" />.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static KafkaBuilder WithSubscriptionSummaryHealthCheck(this KafkaBuilder builder,
        IEnumerable<string>? tags = null,
        HealthStatus? failureStatus = null)
    {
        return builder.WithSummaryHealthCheck(SubscriptionMonitor.Prefix, tags, failureStatus);
    }

    private static KafkaBuilder WithSummaryHealthCheck(this KafkaBuilder builder,
        string name,
        IEnumerable<string>? tags,
        HealthStatus? failureStatus)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.TryAddSingleton<PubSubSummaryHealthCheck>();

        builder.Services.AddHealthChecks().AddCheck<PubSubSummaryHealthCheck>(
            name, tags: tags, failureStatus: failureStatus, timeout: null);

        return builder;
    }

    private static void TryRegisterHandler(this IServiceCollection services, Type type, ServiceLifetime lifetime)
    {
        ServiceDescriptor? existing = services.FirstOrDefault(x => x.ServiceType == type && x.Lifetime != lifetime);

        if (existing != null)
        {
            throw new InvalidOperationException(
                $"Handler {type} already registered with another lifetime ({existing.Lifetime})");
        }

        services.TryAdd(new ServiceDescriptor(type, type, lifetime));
    }

    private static PubSubContext GetOrCreateContext(this KafkaBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var result = (PubSubContext?)builder.Services
            .SingleOrDefault(x => x.ServiceType == typeof(PubSubContext) && x.ImplementationInstance is PubSubContext)
            ?.ImplementationInstance;

        if (result == null)
        {
            result = new PubSubContext();

            builder.Services.AddSingleton(result);

            builder.Services.AddSingleton<IConfigureOptions<SubscriptionOptions>, SubscriptionOptionsConfigure>();
            builder.Services.AddSingleton<IValidateOptions<SubscriptionOptions>, SubscriptionOptionsValidate>();

            builder.Services.AddSingleton<IConfigureOptions<PublicationOptions>, PublicationOptionsConfigure>();
            builder.Services.AddSingleton<IValidateOptions<PublicationOptions>, PublicationOptionsValidate>();
        }

        return result;
    }
}