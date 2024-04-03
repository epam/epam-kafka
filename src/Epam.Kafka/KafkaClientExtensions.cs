// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

namespace Epam.Kafka;

/// <summary>
///     Extensions methods to create dependent admin client and producer.
/// </summary>
public static class KafkaClientExtensions
{
    /// <summary>
    ///     Create new <see cref="IProducer{TKey,TValue}" /> instance reusing existing librdkafka handler.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="client">
    ///     The <see cref="IClient" /> instance with librdkafka handler. Should be from another Producer
    ///     instance.
    /// </param>
    /// <param name="configure">Action to configure <see cref="DependentProducerBuilder{TKey,TValue}" /></param>
    /// <returns>The new <see cref="IProducer{TKey,TValue}" /> instance.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IProducer<TKey, TValue> CreateDependentProducer<TKey, TValue>(this IClient client,
        Action<DependentProducerBuilder<TKey, TValue>>? configure = null)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        var builder = new DependentProducerBuilder<TKey, TValue>(client.Handle);

        configure?.Invoke(builder);

        return builder.Build();
    }

    /// <summary>
    ///     Create new <see cref="IAdminClient" /> instance reusing existing librdkafka handler.
    /// </summary>
    /// <param name="client">The <see cref="IClient" /> instance with librdkafka handler.</param>
    /// <returns>The new <see cref="IAdminClient" /> instance.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IAdminClient CreateDependentAdminClient(this IClient client)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        return new DependentAdminClientBuilder(client.Handle).Build();
    }
}