// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Options;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Epam.Kafka.PubSub.Common;

/// <summary>
///     Fluent builder to configure subscription or publication.
/// </summary>
public abstract class PubSubBuilder<TBuilder, TOptions>
    where TBuilder : PubSubBuilder<TBuilder, TOptions>
    where TOptions : PubSubOptions
{
    private readonly OptionsBuilder<TOptions> _options;

    internal PubSubBuilder(KafkaBuilder builder, Type handlerType, string name, Type keyType, Type valueType)
    {
        if (handlerType == null) throw new ArgumentNullException(nameof(handlerType));

        this.Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        this.Key = name ?? throw new ArgumentNullException(nameof(name));

        this._options = builder.Services.AddOptions<TOptions>(this.Key)
            .Configure(x =>
            {
                x.KeyType = keyType;
                x.ValueType = valueType;
                x.HandlerType = handlerType;
            });

        this.Builder.Services.Add(new ServiceDescriptor(typeof(IHostedService), this.Build, ServiceLifetime.Singleton));
    }

    /// <summary>
    ///     The <see cref="KafkaBuilder" />.
    /// </summary>
    public KafkaBuilder Builder { get; }

    /// <summary>
    ///     The name associated with subscription or publication.
    /// </summary>
    public string Key { get; }

    internal abstract IHostedService Build(IServiceProvider sp);

    /// <summary>
    ///     Wait for dependencies before starting processing pipeline.
    /// </summary>
    /// <param name="waitFor">The factory to create <see cref="Task" /> that represent dependency.</param>
    /// <returns>The builder.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public TBuilder WaitFor(Func<IServiceProvider, Task> waitFor)
    {
        if (waitFor == null)
        {
            throw new ArgumentNullException(nameof(waitFor));
        }

        return this.WaitFor((sp, ct) => waitFor(sp));
    }

    /// <summary>
    ///     Wait for dependencies before starting processing pipeline.
    /// </summary>
    /// <param name="waitFor">The factory to create <see cref="Task" /> that represent dependency.</param>
    /// <returns>The builder.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public TBuilder WaitFor(Func<IServiceProvider, CancellationToken, Task> waitFor)
    {
        if (waitFor == null)
        {
            throw new ArgumentNullException(nameof(waitFor));
        }

        this._options.Configure(x => x.WaitForDependencies.Add(waitFor));

        return (TBuilder)this;
    }

    /// <summary>
    ///     Configure subscription or publication options.
    /// </summary>
    /// <param name="configure">Configuration action.</param>
    /// <returns>The builder.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public TBuilder WithOptions(Action<TOptions> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        this._options.Configure(configure);
        return (TBuilder)this;
    }

    /// <inheritdoc cref="WithOptions"/>
    /// <typeparam name="TDep"><inheritdoc cref="OptionsBuilder{TOptions}.Configure{TDep}" path="/typeparam[@name='TDep']"/> </typeparam>
    public TBuilder WithOptions<TDep>(Action<TOptions, TDep> configure) where TDep : class
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        this._options.Configure(configure);

        return (TBuilder)this;
    }
}