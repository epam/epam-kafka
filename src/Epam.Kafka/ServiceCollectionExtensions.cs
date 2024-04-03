// Copyright © 2024 EPAM Systems

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Epam.Kafka;

/// <summary>
///     Extension methods to configure an <see cref="IServiceCollection" /> for <see cref="IKafkaFactory" />.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Adds the <see cref="IKafkaFactory" /> and related services to the <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <param name="useConfiguration">
    ///     If true adds named configs from <see cref="IConfiguration" /> registered in
    ///     <see cref="IServiceCollection" />.
    /// </param>
    /// <returns>The <see cref="KafkaBuilder" /></returns>
    public static KafkaBuilder AddKafka(this IServiceCollection services, bool useConfiguration = true)
    {
        if (services.SingleOrDefault(x => x.ServiceType == typeof(KafkaBuilder))?.ImplementationInstance is not
            KafkaBuilder result)
        {
            result = new KafkaBuilder(services, useConfiguration);
            services.AddSingleton(result);
        }

        return result;
    }
}