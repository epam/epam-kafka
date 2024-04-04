// Copyright © 2024 EPAM Systems

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using System.Diagnostics;

using Xunit.Abstractions;

namespace Epam.Kafka.Tests.Common;

public abstract class TestWithServices : IDisposable, ILoggingBuilder
{
    private readonly ConfigurationBuilder _configurationBuilder = new();
    private readonly Lazy<ServiceProvider> _serviceProvider;
    private readonly ServiceCollection _services = new();

    protected TestWithServices(ITestOutputHelper output)
    {
        this.Output = output;

        this._services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddProvider(new TestLoggerProvider(this.Output)).AddFilter((s, l) =>
                !(s?.StartsWith("Microsoft", StringComparison.Ordinal) ?? false) || l >= LogLevel.Debug);
        });

        this._services.AddSingleton<IConfiguration>(_ => this._configurationBuilder.Build());

        this._serviceProvider = new Lazy<ServiceProvider>(() => this._services.BuildServiceProvider(true),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public ITestOutputHelper Output { get; }
    public IServiceProvider ServiceProvider => this._serviceProvider.Value;

    public IConfigurationBuilder ConfigurationBuilder => this._serviceProvider.IsValueCreated
        ? throw new InvalidOperationException("Service Provider already created")
        : this._configurationBuilder;

    public ILoggingBuilder LoggingBuilder => this;
    public IKafkaFactory KafkaFactory => this.ServiceProvider.GetRequiredService<IKafkaFactory>();

    public CancellationTokenSource Ctc { get; } = new(TimeSpan.FromSeconds(Debugger.IsAttached ? 300 : 60));

    public ILogger Logger =>
        (ILogger)this.ServiceProvider.GetRequiredService(typeof(ILogger<>).MakeGenericType(this.GetType()));

    public string AnyTopicName { get; } = "T" + Guid.NewGuid().ToString("N");

    public void Dispose()
    {
        this.Ctc.Dispose();

        if (this._serviceProvider.IsValueCreated)
        {
            this._serviceProvider.Value.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    public IServiceCollection Services => this._serviceProvider.IsValueCreated
        ? throw new InvalidOperationException("Service Provider already created")
        : this._services;
}