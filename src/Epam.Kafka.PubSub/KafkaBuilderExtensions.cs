// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common;
using Epam.Kafka.PubSub.Common.HealthChecks;
using Epam.Kafka.PubSub.Publication;
using Epam.Kafka.PubSub.Publication.Options;
using Epam.Kafka.PubSub.Subscription;
using Epam.Kafka.PubSub.Subscription.Options;

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
    /// <param name="name">The name to identity subscription.</param>
    /// <param name="handlerLifetime">
    ///     The <see cref="ServiceLifetime" /> for <see cref="ISubscriptionHandler{TKey,TValue}" />
    ///     implementation. Default <c>ServiceLifetime.Transient</c>
    /// </param>
    /// <typeparam name="TKey">The message key type.</typeparam>
    /// <typeparam name="TValue">The message value type.</typeparam>
    /// <typeparam name="THandler">The <see cref="ISubscriptionHandler{TKey,TValue}" /> implementation type.</typeparam>
    /// <returns>The <see cref="SubscriptionBuilder{TKey,TValue,THandler}" /> to configure subscription behaviour.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static SubscriptionBuilder<TKey, TValue, THandler> AddSubscription<TKey, TValue, THandler>(
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

        TryRegisterHandler(builder.Services, typeof(THandler), handlerLifetime);

        return new SubscriptionBuilder<TKey, TValue, THandler>(builder, name);
    }

    /// <summary>
    ///     Add background service that will produce message to kafka using <see cref="IPublicationHandler{TKey,TValue}" /> to
    ///     get messages and manage their state.
    /// </summary>
    /// <param name="builder">The <see cref="KafkaBuilder" />.</param>
    /// <param name="name">The name to identity publication.</param>
    /// <param name="handlerLifetime">
    ///     The <see cref="ServiceLifetime" /> for <see cref="IPublicationHandler{TKey,TValue}" />
    ///     implementation. Default <c>ServiceLifetime.Transient</c>
    /// </param>
    /// <typeparam name="TKey">The message key type.</typeparam>
    /// <typeparam name="TValue">The message value type.</typeparam>
    /// <typeparam name="THandler">The <see cref="IPublicationHandler{TKey,TValue}" /> implementation type.</typeparam>
    /// <returns>The <see cref="PublicationBuilder{TKey,TValue,THandler}" /> to configure publication behaviour.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static PublicationBuilder<TKey, TValue, THandler> AddPublication<TKey, TValue, THandler>(
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

        TryRegisterHandler(builder.Services, typeof(THandler), handlerLifetime);

        return new PublicationBuilder<TKey, TValue, THandler>(builder, name);
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
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (builder.Services.All(x => x.ServiceType != typeof(PubSubSummaryHealthCheck)))
        {
            builder.Services.AddSingleton<PubSubSummaryHealthCheck>();

            builder.Services.AddHealthChecks().AddCheck<PubSubSummaryHealthCheck>(
                PubSubSummaryHealthCheck.Name, tags: tags, failureStatus: failureStatus,
                timeout: null);
        }

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