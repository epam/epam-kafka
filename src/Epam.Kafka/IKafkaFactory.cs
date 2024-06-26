// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Confluent.SchemaRegistry;

using Microsoft.Extensions.DependencyInjection;

namespace Epam.Kafka;

/// <summary>
///     A factory abstraction for a component that can create <see cref="IConsumer{TKey,TValue}" />,
///     <see cref="IProducer{TKey,TValue}" />, <see cref="ISharedClient" />, <see cref="ISchemaRegistryClient" />,
///     <see cref="ConsumerConfig" /> , <see cref="ProducerConfig" /> instances with custom
///     configuration for a given logical name.
/// </summary>
/// <remarks>
///     A default <see cref="IKafkaFactory" /> can be registered in an <see cref="IServiceCollection" />
///     by calling <see cref="ServiceCollectionExtensions.AddKafka(IServiceCollection,bool)" />.
///     The default <see cref="IKafkaFactory" /> will be registered in the service collection as a singleton.
/// </remarks>
public interface IKafkaFactory
{
    /// <summary>
    ///     Creates and configures an <see cref="ConsumerConfig" /> instance using the configuration that corresponds
    ///     to the logical name specified by <paramref name="configName" />.
    /// </summary>
    /// <param name="configName">The logical name of the <see cref="ConsumerConfig" /> to create.</param>
    /// <returns>A new <see cref="ConsumerConfig" /> instance.</returns>
    /// <remarks>
    ///     <para>
    ///         Each call to <see cref="CreateConsumerConfig(string)" /> is guaranteed to return a new
    ///         <see cref="ConsumerConfig" />  instance.
    ///     </para>
    ///     <para>
    ///         Callers are also free to mutate the returned <see cref="ConsumerConfig" /> instance's public properties  as
    ///         desired.
    ///     </para>
    /// </remarks>
    ConsumerConfig CreateConsumerConfig(string? configName = null);

    /// <summary>
    ///     Creates and configures an <see cref="ProducerConfig" /> instance using the configuration that corresponds
    ///     to the logical name specified by <paramref name="configName" />.
    /// </summary>
    /// <param name="configName">The logical name of the <see cref="ProducerConfig" /> to create.</param>
    /// <returns>A new <see cref="ProducerConfig" /> instance.</returns>
    /// <remarks>
    ///     <para>
    ///         Each call to <see cref="CreateProducerConfig(string)" /> is guaranteed to return a new
    ///         <see cref="ProducerConfig" />  instance.
    ///     </para>
    ///     <para>
    ///         Callers are also free to mutate the returned <see cref="ProducerConfig" /> instance's public properties as
    ///         desired.
    ///     </para>
    /// </remarks>
    ProducerConfig CreateProducerConfig(string? configName = null);

    /// <summary>
    ///     Creates and configures an <see cref="IConsumer{TKey,TValue}" /> instance using
    ///     <see cref="ConsumerConfig" /> specified by <paramref name="config" /> and
    ///     <see cref="ClientConfig" />  that corresponds to the logical name specified by <paramref name="cluster" />.
    /// </summary>
    /// <param name="config">The <see cref="ConsumerConfig" /> instance.</param>
    /// <param name="cluster">The logical name of the <see cref="ClientConfig" />.</param>
    /// <param name="configure">Action to configure <see cref="ConsumerBuilder{TKey,TValue}" /></param>
    /// <returns>A new <see cref="IConsumer{TKey,TValue}" /> instance.</returns>
    /// <remarks>
    ///     <para>
    ///         Each call to <see cref="CreateConsumer{TKey,TValue}" /> is guaranteed to return a new
    ///         <see cref="IConsumer{TKey,TValue}" /> instance.
    ///     </para>
    /// </remarks>
    IConsumer<TKey, TValue> CreateConsumer<TKey, TValue>(ConsumerConfig config, string? cluster = null,
        Action<ConsumerBuilder<TKey, TValue>>? configure = null);

    /// <summary>
    ///     Creates and configures an <see cref="IProducer{TKey,TValue}" /> instance using
    ///     <see cref="ProducerConfig" /> specified by <paramref name="config" /> and
    ///     <see cref="ClientConfig" />  that corresponds to the logical name specified by <paramref name="cluster" />.
    /// </summary>
    /// <param name="config">The <see cref="ProducerConfig" /> instance.</param>
    /// <param name="cluster">The logical name of the <see cref="ClientConfig" />.</param>
    /// <param name="configure">Action to configure <see cref="ProducerBuilder{TKey,TValue}" /></param>
    /// <returns>A new <see cref="IProducer{TKey,TValue}" /> instance.</returns>
    /// <remarks>
    ///     <para>
    ///         Each call to <see cref="CreateProducer{TKey,TValue}" /> is guaranteed to return a new
    ///         <see cref="IProducer{TKey,TValue}" /> instance.
    ///     </para>
    /// </remarks>
    IProducer<TKey, TValue> CreateProducer<TKey, TValue>(ProducerConfig config, string? cluster = null,
        Action<ProducerBuilder<TKey, TValue>>? configure = null);

    /// <summary>
    ///     Creates new or return existing an <see cref="ISharedClient" /> instance using
    ///     <see cref="ClientConfig" /> that corresponds to the logical name specified by <paramref name="cluster" />.
    /// </summary>
    /// <param name="cluster">The logical name of the <see cref="ClientConfig" />.</param>
    /// <returns>An <see cref="ISharedClient" /> instance.</returns>
    /// <remarks>
    ///     <para>
    ///         First call to <see cref="GetOrCreateClient(string)" /> with given <paramref name="cluster" /> value is
    ///         guaranteed to return a new <see cref="ISharedClient" /> instance and cache it using <paramref name="cluster" /> value
    ///         as a key.
    ///         All subsequent calls with same <paramref name="cluster" /> value will return cached instance.
    ///     </para>
    ///     <para>
    ///         <see cref="ISharedClient" /> implements <see cref="IDisposable" />, so you can safely call
    ///         <see cref="IDisposable.Dispose" /> even if instances returned from cache, however it's not necessary.
    ///         Lifetime of the clients created by <see cref="GetOrCreateClient(string)" /> controlled by
    ///         <see cref="IKafkaFactory" />. All of them will be disposed together with <see cref="IKafkaFactory" /> default
    ///         implementation.
    ///     </para>
    /// </remarks>
    ISharedClient GetOrCreateClient(string? cluster = null);

    /// <summary>
    ///     Creates new or return existing an <see cref="ISchemaRegistryClient" /> instance using
    ///     <see cref="SchemaRegistryConfig" /> that corresponds to the logical name specified by <paramref name="cluster" />.
    /// </summary>
    /// <param name="cluster">The logical name of the <see cref="SchemaRegistryConfig" />.</param>
    /// <returns>An <see cref="ISchemaRegistryClient" /> instance.</returns>
    /// <remarks>
    ///     <para>
    ///         First call to <see cref="GetOrCreateSchemaRegistryClient(string)" /> with given <paramref name="cluster" />
    ///         value is guaranteed to return a new <see cref="ISchemaRegistryClient" /> instance and cache it using
    ///         <paramref name="cluster" /> value as a key.
    ///         All subsequent calls with same <paramref name="cluster" /> value will return cached instance.
    ///     </para>
    ///     <para>
    ///         <see cref="ISchemaRegistryClient" /> implements <see cref="IDisposable" />, so you can safely call
    ///         <see cref="IDisposable.Dispose" /> even if instances returned from cache, however it's not necessary.
    ///         Lifetime of the clients created by <see cref="GetOrCreateSchemaRegistryClient(string)" /> controlled by
    ///         <see cref="IKafkaFactory" />. All of them will be disposed together with <see cref="IKafkaFactory" /> default
    ///         implementation.
    ///     </para>
    /// </remarks>
    ISchemaRegistryClient GetOrCreateSchemaRegistryClient(string? cluster = null);
}