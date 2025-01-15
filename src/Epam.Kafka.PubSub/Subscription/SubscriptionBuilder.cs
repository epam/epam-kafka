// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Confluent.SchemaRegistry;

using Epam.Kafka.PubSub.Common;
using Epam.Kafka.PubSub.Subscription.HealthChecks;
using Epam.Kafka.PubSub.Subscription.Options;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.PubSub.Subscription.State;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Epam.Kafka.PubSub.Subscription;

/// <summary>
///     Fluent API to configure subscription background services behaviour.
/// </summary>
/// <typeparam name="TKey">The kafka message key type.</typeparam>
/// <typeparam name="TValue">The kafka message value type.</typeparam>
public class
    SubscriptionBuilder<TKey, TValue> : PubSubBuilder<SubscriptionBuilder<TKey, TValue>, SubscriptionOptions>
{
    internal SubscriptionBuilder(KafkaBuilder builder, string name, Type handlerType)
        : base(builder, handlerType, name, typeof(TKey), typeof(TValue))
    {
        this.Builder.Services.TryAddSingleton<InternalKafkaState>();
        this.Builder.Services.TryAddTransient(typeof(ExternalState<>));
        this.Builder.Services.TryAddTransient(typeof(CombinedState<>));
    }

    /// <summary>
    ///     Set factory for creation of key deserializer for consumer.
    /// </summary>
    /// <param name="configure">
    ///     Factory to create deserializer for kafka message key of type <typeparamref name="TKey" />
    /// </param>
    /// <returns>The <see cref="SubscriptionBuilder{TKey,TValue}" />.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public SubscriptionBuilder<TKey, TValue> WithKeyDeserializer(
        Func<Lazy<ISchemaRegistryClient>, IDeserializer<TKey>> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        this.WithOptions(x => x.KeyDeserializer = configure);

        return this;
    }

    /// <summary>
    ///     Set factory for creation of value deserializer for consumer.
    /// </summary>
    /// <param name="configure">
    ///     Factory to create deserializer for kafka message value of type <typeparamref name="TValue" />
    /// </param>
    /// <returns>The <see cref="SubscriptionBuilder{TKey,TValue}" />.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public SubscriptionBuilder<TKey, TValue> WithValueDeserializer(
        Func<Lazy<ISchemaRegistryClient>, IDeserializer<TValue>> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        this.WithOptions(x => x.ValueDeserializer = configure);

        return this;
    }

    internal override IHostedService CreateInstance(IServiceProvider sp, SubscriptionOptions options)
    {
        return new SubscriptionBackgroundService<TKey, TValue>(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IKafkaFactory>(),
            options,
            sp.GetRequiredService<PubSubContext>().Subscriptions[this.Key],
            sp.GetService<ILoggerFactory>());
    }

    /// <summary>
    ///     Use <see cref="IConsumer{TKey,TValue}.Assign(Confluent.Kafka.TopicPartition)"/> to assign topic partitions from <see cref="SubscriptionOptions.Topics"/>.
    ///     Configure external offsets storage based on <typeparamref name="TOffsetsStorage" /> implementation of
    ///     <see cref="IExternalOffsetsStorage" />.
    ///     <remarks>
    ///         Implementation of <see cref="IExternalOffsetsStorage" /> is the single point of truth for offsets.
    ///         Optionally <see cref="SubscriptionOptions.ExternalStateCommitToKafka" /> can be used to configure additional
    ///         commit to kafka internal state (useful for monitoring purposes).
    ///     </remarks>
    /// </summary>
    /// <typeparam name="TOffsetsStorage">The <see cref="IExternalOffsetsStorage" /> implementation type.</typeparam>
    /// <returns>The <see cref="SubscriptionBuilder{TKey,TValue}" />.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public SubscriptionBuilder<TKey, TValue> WithAssignAndExternalOffsets<TOffsetsStorage>()
        where TOffsetsStorage : IExternalOffsetsStorage
    {
        if (this.Builder.Services.All(x => x.ServiceType != typeof(TOffsetsStorage)))
        {
            throw new InvalidOperationException($"Type '{typeof(TOffsetsStorage)}' should be registered first.");
        }

        this.WithOptions(x => x.StateType = typeof(ExternalState<>).MakeGenericType(typeof(TOffsetsStorage)));

        return this;
    }

    /// <summary>
    ///     Use <see cref="IConsumer{TKey,TValue}.Assign(Confluent.Kafka.TopicPartition)"/> to assign topic partitions from <see cref="SubscriptionOptions.Topics"/>.
    ///     Configure external offsets storage based on <see cref="IExternalOffsetsStorage" /> registered in
    ///     <see cref="IServiceCollection" />.
    ///     <remarks>
    ///         <inheritdoc cref="WithAssignAndExternalOffsets{TOffsetsStorage}" />
    ///     </remarks>
    /// </summary>
    /// <returns>The <see cref="SubscriptionBuilder{TKey,TValue}" />.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public SubscriptionBuilder<TKey, TValue> WithAssignAndExternalOffsets()
    {
        return this.WithAssignAndExternalOffsets<IExternalOffsetsStorage>();
    }

    /// <summary>
    ///     Use <see cref="IConsumer{TKey,TValue}.Subscribe(IEnumerable{string})"/> to subscribe for topics from <see cref="SubscriptionOptions.Topics"/>.
    ///     Partition assignment will be controlled by kafka broker.
    ///     Configure external offsets storage based on <typeparamref name="TOffsetsStorage" /> implementation of
    ///     <see cref="IExternalOffsetsStorage" />.
    ///     <remarks>
    ///         Implementation of <see cref="IExternalOffsetsStorage" /> is the single point of truth for offsets.
    ///         Optionally <see cref="SubscriptionOptions.ExternalStateCommitToKafka" /> can be used to configure additional
    ///         commit to kafka internal state (useful for monitoring purposes).
    ///         Default partition assignment strategy is <see cref="PartitionAssignmentStrategy.CooperativeSticky"/>.
    ///         Can be changed by configuring <see cref="ConsumerConfig.PartitionAssignmentStrategy"/> for corresponding consumer.
    ///     </remarks>
    /// </summary>
    /// <typeparam name="TOffsetsStorage">The <see cref="IExternalOffsetsStorage" /> implementation type.</typeparam>
    /// <returns>The <see cref="SubscriptionBuilder{TKey,TValue}" />.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public SubscriptionBuilder<TKey, TValue> WithSubscribeAndExternalOffsets<TOffsetsStorage>()
        where TOffsetsStorage : IExternalOffsetsStorage
    {
        if (this.Builder.Services.All(x => x.ServiceType != typeof(TOffsetsStorage)))
        {
            throw new InvalidOperationException($"Type '{typeof(TOffsetsStorage)}' should be registered first.");
        }

        this.WithOptions(x => x.StateType = typeof(CombinedState<>).MakeGenericType(typeof(TOffsetsStorage)));

        return this;
    }

    /// <summary>
    ///     Use <see cref="IConsumer{TKey,TValue}.Subscribe(IEnumerable{string})"/> to subscribe for topics from <see cref="SubscriptionOptions.Topics"/>.
    ///     Partition assignment will be controlled by kafka broker.
    ///     Configure external offsets storage based on <see cref="IExternalOffsetsStorage" /> registered in
    ///     <see cref="IServiceCollection" />.
    ///     <remarks>
    ///         <inheritdoc cref="WithSubscribeAndExternalOffsets{TOffsetsStorage}" />
    ///     </remarks>
    /// </summary>
    /// <returns>The <see cref="SubscriptionBuilder{TKey,TValue}" />.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public SubscriptionBuilder<TKey, TValue> WithSubscribeAndExternalOffsets()
    {
        return this.WithSubscribeAndExternalOffsets<IExternalOffsetsStorage>();
    }

    /// <summary>
    ///     Register health check for subscription.
    /// </summary>
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
    /// <returns>The <see cref="SubscriptionBuilder{TKey,TValue}" /></returns>
    public SubscriptionBuilder<TKey, TValue> WithHealthChecks(
        IEnumerable<string>? tags = null,
        HealthStatus? failureStatus = null)
    {
        this.Builder.Services.AddHealthChecks().Add(new HealthCheckRegistration(
            SubscriptionMonitor.BuildFullName(this.Key), sp => new SubscriptionHealthCheck(
                sp.GetRequiredService<IOptionsMonitor<SubscriptionOptions>>(), sp.GetRequiredService<PubSubContext>(),
                this.Key), failureStatus, tags));

        return this;
    }
}