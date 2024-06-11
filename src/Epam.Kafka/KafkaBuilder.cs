// Copyright © 2024 EPAM Systems

using Epam.Kafka.Internals;
using Epam.Kafka.Options;
using Epam.Kafka.Options.Configuration;
using Epam.Kafka.Options.Validation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Epam.Kafka;

/// <summary>
///     A builder for configuring named client, consumer, producer instances returned by <see cref="IKafkaFactory" />.
/// </summary>
public partial class KafkaBuilder
{
    private readonly bool _useConfiguration;
    private readonly OptionsBuilder<KafkaFactoryOptions> _factoryOptions;


    internal KafkaBuilder(IServiceCollection services, bool useConfiguration)
    {
        this._useConfiguration = useConfiguration;
        this.Services = services ?? throw new ArgumentNullException(nameof(services));

        this._factoryOptions = services.AddOptions<KafkaFactoryOptions>();

        if (useConfiguration)
        {
            this.Services.AddSingleton<IConfigureOptions<KafkaFactoryOptions>, FactoryOptionsConfigure>();
            this.Services.AddSingleton<IConfigureOptions<KafkaConsumerOptions>, ConsumerOptionsConfigure>();
            this.Services.AddSingleton<IConfigureOptions<KafkaProducerOptions>, ProducerOptionsConfigure>();
            this.Services.AddSingleton<IConfigureOptions<KafkaClusterOptions>, ClusterOptionsConfigure>();
        }

        this.Services.AddSingleton<IValidateOptions<KafkaConsumerOptions>, ConsumerValidation>();
        this.Services.AddSingleton<IValidateOptions<KafkaProducerOptions>, ProducerValidation>();
        this.Services.AddSingleton<IValidateOptions<KafkaClusterOptions>, ClusterValidation>();

        services.AddSingleton<IKafkaFactory, KafkaFactory>();
    }

    /// <summary>
    ///     The <see cref="IServiceCollection" />.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    ///     Add <see cref="KafkaClusterOptions" /> with logical name specified by <paramref name="name" />.
    /// </summary>
    /// <param name="name">The logical name.</param>
    /// <returns>Options builder for further configuration.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public OptionsBuilder<KafkaClusterOptions> WithClusterConfig(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        return this.Services.AddOptions<KafkaClusterOptions>(name);
    }

    /// <summary>
    ///     Add <see cref="KafkaConsumerOptions" /> with logical name specified by <paramref name="name" />.
    /// </summary>
    /// <param name="name">The logical name.</param>
    /// <returns>Options builder for further configuration.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public OptionsBuilder<KafkaConsumerOptions> WithConsumerConfig(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        return this.Services.AddOptions<KafkaConsumerOptions>(name);
    }

    /// <summary>
    ///     Add <see cref="KafkaProducerOptions" /> with logical name specified by <paramref name="name" />.
    /// </summary>
    /// <param name="name">The logical name.</param>
    /// <returns>Options builder for further configuration.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public OptionsBuilder<KafkaProducerOptions> WithProducerConfig(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        return this.Services.AddOptions<KafkaProducerOptions>(name);
    }

    /// <summary>
    ///     Configure <see cref="KafkaFactoryOptions" />.
    /// </summary>
    /// <param name="configure">The configuration action</param>
    /// <returns>The <see cref="KafkaBuilder" /></returns>
    public KafkaBuilder WithDefaults(Action<KafkaFactoryOptions> configure)
    {
        this._factoryOptions.Configure(configure);

        return this;
    }
}