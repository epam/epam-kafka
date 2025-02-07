﻿// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Confluent.SchemaRegistry;

using Epam.Kafka.Internals.Observable;
using Epam.Kafka.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Epam.Kafka.Internals;

internal sealed class KafkaFactory : IKafkaFactory, IDisposable
{
    private const string LoggerCategoryName = "Epam.Kafka.Factory";

    private readonly Dictionary<KafkaClusterOptions, SharedClient> _clients = new();
    private readonly IOptionsMonitor<KafkaClusterOptions> _clusterOptions;
    private readonly IOptionsMonitor<KafkaConsumerOptions> _consumerOptions;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IOptionsMonitor<KafkaProducerOptions> _producerOptions;
    private readonly Dictionary<KafkaClusterOptions, CachedSchemaRegistryClient> _registries = new();
    private readonly object _syncObj = new();
    private readonly IOptionsMonitor<KafkaFactoryOptions> _topicOptions;
    private bool _disposed;

    internal HashSet<string> UsedClusters { get; } = new();

    public KafkaFactory(
        IOptionsMonitor<KafkaFactoryOptions> topicOptions,
        IOptionsMonitor<KafkaClusterOptions> clusterOptions,
        IOptionsMonitor<KafkaConsumerOptions> consumerOptions,
        IOptionsMonitor<KafkaProducerOptions> producerOptions,
        ILoggerFactory? loggerFactory = null)
    {
        this._topicOptions = topicOptions ?? throw new ArgumentNullException(nameof(topicOptions));
        this._clusterOptions = clusterOptions ?? throw new ArgumentNullException(nameof(clusterOptions));
        this._consumerOptions = consumerOptions ?? throw new ArgumentNullException(nameof(consumerOptions));
        this._producerOptions = producerOptions ?? throw new ArgumentNullException(nameof(producerOptions));
        this._loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    }

    public void Dispose()
    {
        this._disposed = true;

        lock (this._syncObj)
        {
            foreach (KeyValuePair<KafkaClusterOptions, SharedClient> producer in this._clients)
            {
                producer.Value.DisposeInternal();
            }

            foreach (KeyValuePair<KafkaClusterOptions, CachedSchemaRegistryClient> producer in this._registries)
            {
                producer.Value.Dispose();
            }
        }
    }

    public ConsumerConfig CreateConsumerConfig(string? configName = null)
    {
        this.CheckIfDisposed();

        configName ??= this._topicOptions.CurrentValue.Consumer;

        ValidateLogicalName(configName, "consumer");

        try
        {
            KafkaConsumerOptions options = this._consumerOptions.Get(configName);

            return new ConsumerConfig(options.ConsumerConfig.ToDictionary(x => x.Key, x => x.Value));
        }
        catch (OptionsValidationException e)
        {
            throw new InvalidOperationException(
                $"Consumer config '{configName}' in corrupted state: {e.Message}", e);
        }
    }

    public ProducerConfig CreateProducerConfig(string? configName = null)
    {
        this.CheckIfDisposed();

        configName ??= this._topicOptions.CurrentValue.Producer;

        ValidateLogicalName(configName, "producer");

        try
        {
            KafkaProducerOptions options = this._producerOptions.Get(configName);

            return new ProducerConfig(options.ProducerConfig.ToDictionary(x => x.Key, x => x.Value));
        }
        catch (Exception e)
        {
            throw new InvalidOperationException(
                $"Producer config '{configName}' in corrupted state: {e.Message}", e);
        }
    }

    public IConsumer<TKey, TValue> CreateConsumer<TKey, TValue>(ConsumerConfig config, string? cluster = null,
        Action<ConsumerBuilder<TKey, TValue>>? configure = null)
    {
        this.CheckIfDisposed();

        KafkaClusterOptions clusterOptions = this.GetAndValidateClusterOptions(cluster);

        Dictionary<string, string> resultConfig = MergeResultConfig(clusterOptions, config);

        config = new ConsumerConfig(resultConfig);

        // Init logger category from config and remove key because it is not standard key and cause errors.
        string logHandler = config.GetDotnetLoggerCategory();
        resultConfig.Remove(KafkaConfigExtensions.DotnetLoggerCategoryKey);

        var builder = new ConsumerBuilder<TKey, TValue>(config);

        configure?.Invoke(builder);

        bool oauthSet = false;
        bool logSet = false;

        try
        {
            builder.SetLogHandler((_, m) => this._loggerFactory.CreateLogger(logHandler).KafkaLogHandler(m));
            logSet = true;
        }
        catch (InvalidOperationException)
        {
            // handler already set
        }

        if (clusterOptions is { OauthHandler: { }, ClientConfig.SaslMechanism: SaslMechanism.OAuthBearer })
        {
            try
            {
                builder.SetOAuthBearerTokenRefreshHandler(clusterOptions.OauthHandler.Invoke);
                oauthSet = true;
            }
            catch (InvalidOperationException)
            {
                // handler already set
                if (clusterOptions.OauthHandlerThrow)
                {
                    throw;
                }
            }
        }

        ILogger logger = this._loggerFactory.CreateLogger(LoggerCategoryName);

        ObservableConsumer<TKey, TValue> consumer;

        try
        {
            consumer = new ObservableConsumer<TKey, TValue>(builder, config);

            logger.ConsumerCreateOk(PrepareConfigForLogs(config), typeof(TKey), typeof(TValue), oauthSet, logSet);
        }
        catch (Exception exc)
        {
            logger.ConsumerCreateError(exc, PrepareConfigForLogs(config), typeof(TKey), typeof(TValue), oauthSet, logSet);

            throw;
        }

        return consumer;
    }

    public IProducer<TKey, TValue> CreateProducer<TKey, TValue>(ProducerConfig config, string? cluster = null,
        Action<ProducerBuilder<TKey, TValue>>? configure = null)
    {
        this.CheckIfDisposed();

        KafkaClusterOptions clusterOptions = this.GetAndValidateClusterOptions(cluster);

        Dictionary<string, string> resultConfig = MergeResultConfig(clusterOptions, config);

        config = new ProducerConfig(resultConfig);

        // Init logger category from config and remove key because it is not standard key and cause errors.
        string logHandler = config.GetDotnetLoggerCategory();
        resultConfig.Remove(KafkaConfigExtensions.DotnetLoggerCategoryKey);

        ProducerBuilder<TKey, TValue> builder = new(config);

        configure?.Invoke(builder);

        bool oauthSet = false;
        bool logSet = false;

        try
        {
            builder.SetLogHandler((_, m) => this._loggerFactory.CreateLogger(logHandler).KafkaLogHandler(m));
            logSet = true;
        }
        catch (InvalidOperationException)
        {
            // handler already set
        }

        if (clusterOptions is { OauthHandler: { }, ClientConfig.SaslMechanism: SaslMechanism.OAuthBearer })
        {
            try
            {
                builder.SetOAuthBearerTokenRefreshHandler(clusterOptions.OauthHandler);
                oauthSet = true;
            }
            catch (InvalidOperationException)
            {
                // handler already set
                if (clusterOptions.OauthHandlerThrow)
                {
                    throw;
                }
            }
        }

        ILogger logger = this._loggerFactory.CreateLogger(LoggerCategoryName);

        ObservableProducer<TKey, TValue> producer;

        try
        {
            producer = new(builder);

            logger.ProducerCreateOk(PrepareConfigForLogs(config), typeof(TKey), typeof(TValue), oauthSet, logSet);
        }
        catch (Exception exc)
        {
            logger.ProducerCreateError(exc, PrepareConfigForLogs(config), typeof(TKey), typeof(TValue), oauthSet, logSet);

            throw;
        }

        return producer;
    }

    public IClient GetOrCreateClient(string? cluster = null)
    {
        this.CheckIfDisposed();

        KafkaClusterOptions clusterOptions = this.GetAndValidateClusterOptions(cluster);

        if (!this._clients.TryGetValue(clusterOptions, out SharedClient? result))
        {
            lock (this._syncObj)
            {
                if (!this._clients.TryGetValue(clusterOptions, out result))
                {
                    result = new SharedClient(this, cluster ?? this._topicOptions.CurrentValue.Cluster);

                    this._clients.Add(clusterOptions, result);
                }
            }
        }

        return result;
    }

    public ISchemaRegistryClient GetOrCreateSchemaRegistryClient(string? cluster = null)
    {
        this.CheckIfDisposed();

        KafkaClusterOptions clusterOptions = this.GetAndValidateClusterOptions(cluster);

        if (!this._registries.TryGetValue(clusterOptions, out CachedSchemaRegistryClient? result))
        {
            ILogger logger = this._loggerFactory.CreateLogger(LoggerCategoryName);

            lock (this._syncObj)
            {
                if (!this._registries.TryGetValue(clusterOptions, out result))
                {
                    try
                    {
                        result = new CachedSchemaRegistryClient(clusterOptions.SchemaRegistryConfig,
                            clusterOptions.AuthenticationHeaderValueProvider);

                        this._registries.Add(clusterOptions, result);

                        logger.RegistryClientCreateOk(PrepareConfigForLogs(clusterOptions.SchemaRegistryConfig),
                            clusterOptions.AuthenticationHeaderValueProvider?.GetType());
                    }
                    catch (Exception exception)
                    {
                        logger.RegistryClientCreateError(exception, PrepareConfigForLogs(clusterOptions.SchemaRegistryConfig),
                            clusterOptions.AuthenticationHeaderValueProvider?.GetType());
                        throw;
                    }
                }
            }
        }

        return result;
    }

    private static void ValidateLogicalName(string? configName, string entityType)
    {
        if (string.IsNullOrWhiteSpace(configName))
        {
            throw new InvalidOperationException($"Unable to create {entityType} with null or whitespace logical name.");
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> PrepareConfigForLogs(IEnumerable<KeyValuePair<string, string>> config)
    {
        return config.Select(x => Contains(x, "password") || Contains(x, "secret")
            ? new KeyValuePair<string, string>(x.Key, "*******")
            : x);

        static bool Contains(KeyValuePair<string, string> x, string value)
        {
            return x.Key.IndexOf(value, StringComparison.OrdinalIgnoreCase) > -1;
        }
    }

    private void CheckIfDisposed()
    {
        if (this._disposed)
        {
            throw new ObjectDisposedException(nameof(KafkaFactory));
        }
    }

    private KafkaClusterOptions GetAndValidateClusterOptions(string? cluster)
    {
        cluster ??= this._topicOptions.CurrentValue.Cluster;

        ValidateLogicalName(cluster, "cluster");

        // save cluster name for further health check
        // https://learn.microsoft.com/en-us/dotnet/standard/collections/thread-safe/
        // If you're only reading from a shared collection, then you can use the classes in the System.Collections.Generic namespace
        if (!this.UsedClusters.Contains(cluster!))
        {
            lock (this.UsedClusters)
            {
                this.UsedClusters.Add(cluster!);
            }
        }

        try
        {
            return this._clusterOptions.Get(cluster);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException(
                $"Cluster config '{cluster}' in corrupted state. See inner exception for details.", e);
        }
    }

    private static Dictionary<string, string> MergeResultConfig(KafkaClusterOptions cluster,
        IEnumerable<KeyValuePair<string, string>> config)
    {
        var result = cluster.ClientConfig.ToDictionary(p => p.Key, p => p.Value);

        foreach (KeyValuePair<string, string> kvp in config)
        {
            result[kvp.Key] = kvp.Value;
        }

        return result;
    }
}