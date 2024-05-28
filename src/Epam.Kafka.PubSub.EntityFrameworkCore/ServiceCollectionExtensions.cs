// Copyright © 2024 EPAM Systems

#if EF6
using Epam.Kafka.PubSub.EntityFramework6.Subscription;
using Epam.Kafka.PubSub.EntityFramework6.Subscription.State;
using System.Data.Entity;
#else
using Epam.Kafka.PubSub.EntityFrameworkCore.Subscription;
using Epam.Kafka.PubSub.EntityFrameworkCore.Subscription.State;
using Microsoft.EntityFrameworkCore;
#endif
using Epam.Kafka.PubSub.Subscription;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

#if EF6
namespace Epam.Kafka.PubSub.EntityFramework6;
#else
namespace Epam.Kafka.PubSub.EntityFrameworkCore;
#endif

/// <summary>
///     Extension methods to configure an <see cref="IServiceCollection" /> for <see cref="IExternalOffsetsStorage" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Register <see cref="IExternalOffsetsStorage" /> default implementation that use db context
    ///     <typeparamref name="TContext" /> to store offsets in database.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <typeparam name="TContext">The db context type that implement <see cref="IKafkaStateDbContext" />.</typeparam>
    /// <returns>The <see cref="IServiceCollection" />.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public static IServiceCollection TryAddKafkaDbContextState<TContext>(
        this IServiceCollection services)
        where TContext : DbContext, IKafkaStateDbContext
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        Type type = typeof(TContext);

        ServiceDescriptor? descriptor = services.FirstOrDefault(x => x.ServiceType == type);

        if (descriptor == null || descriptor.Lifetime == ServiceLifetime.Singleton)
        {
            throw new ArgumentException(
                $"Context {type} should be registered first with '{ServiceLifetime.Scoped}' or '{ServiceLifetime.Transient}' ServiceLifetime.");
        }

        services.TryAdd(new ServiceDescriptor(typeof(IExternalOffsetsStorage),
            typeof(DbContextOffsetsStorage<TContext>),
            descriptor.Lifetime));

        return services;
    }
}