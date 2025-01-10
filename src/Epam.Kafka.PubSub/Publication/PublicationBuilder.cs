// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Confluent.SchemaRegistry;

using Epam.Kafka.PubSub.Common;
using Epam.Kafka.PubSub.Publication.HealthChecks;
using Epam.Kafka.PubSub.Publication.Options;
using Epam.Kafka.PubSub.Publication.Pipeline;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Epam.Kafka.PubSub.Publication;

/// <inheritdoc />
public sealed class
    PublicationBuilder<TKey, TValue> : PubSubBuilder<PublicationBuilder<TKey, TValue>,
        PublicationOptions>
{
    internal PublicationBuilder(KafkaBuilder builder, string name, Type handlerType)
        : base(builder, handlerType, name, typeof(TKey), typeof(TValue))
    {
    }

    /// <summary>
    ///     Set factory for creation of key serializer for producer.
    /// </summary>
    /// <param name="configure">Factory to create serializer for kafka message key of type <typeparamref name="TKey" /></param>
    /// <returns>The <see cref="PublicationBuilder{TKey,TValue}" />.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public PublicationBuilder<TKey, TValue> WithKeySerializer(
        Func<Lazy<ISchemaRegistryClient>, ISerializer<TKey>> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        this.WithOptions(x => x.KeySerializer = configure);

        return this;
    }

    /// <summary>
    ///     Set factory for creation of value serializer for producer.
    /// </summary>
    /// <param name="configure">
    ///     Factory to create serializer for kafka message value of type <typeparamref name="TValue" />
    /// </param>
    /// <returns>The <see cref="PublicationBuilder{TKey,TValue}" />.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public PublicationBuilder<TKey, TValue> WithValueSerializer(
        Func<Lazy<ISchemaRegistryClient>, ISerializer<TValue>> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        this.WithOptions(x => x.ValueSerializer = configure);

        return this;
    }

    /// <summary>
    ///     Configure <see cref="ProducerPartitioner" />.
    /// </summary>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The <see cref="PublicationBuilder{TKey,TValue}" />.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public PublicationBuilder<TKey, TValue> WithPartitioner(Action<ProducerPartitioner> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        this.WithOptions(x => configure(x.Partitioner));

        return this;
    }

    internal override IHostedService CreateInstance(IServiceProvider sp, PublicationOptions options)
    {
        return new PublicationBackgroundService<TKey, TValue>(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IKafkaFactory>(),
            options,
            sp.GetRequiredService<PubSubContext>().Publications[this.Key],
            this.HandlerType,
            sp.GetService<ILoggerFactory>());
    }

    /// <summary>
    ///     Register health check for publication.
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
    /// <returns>The <see cref="PublicationBuilder{TKey,TValue}" /></returns>
    public PublicationBuilder<TKey, TValue> WithHealthChecks(
        IEnumerable<string>? tags = null,
        HealthStatus? failureStatus = null)
    {
        this.Builder.Services.AddHealthChecks().Add(new HealthCheckRegistration(
            PublicationMonitor.BuildFullName(this.Key), sp => new PublicationHealthCheck(
                sp.GetRequiredService<IOptionsMonitor<PublicationOptions>>(), sp.GetRequiredService<PubSubContext>(),
                this.Key), failureStatus, tags));

        return this;
    }
}