// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Confluent.SchemaRegistry;

using Epam.Kafka.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Epam.Kafka.Internals;

internal sealed class KafkaFactory : IKafkaFactory, IDisposable
{
    private readonly Dictionary<KafkaClusterOptions, AdminClient> _clients = new();
    private readonly IOptionsMonitor<KafkaClusterOptions> _clusterOptions;
    private readonly IOptionsMonitor<KafkaConsumerOptions> _consumerOptions;
    private readonly ILogger? _logger;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly IOptionsMonitor<KafkaProducerOptions> _producerOptions;
    private readonly Dictionary<KafkaClusterOptions, CachedSchemaRegistryClient> _registries = new();
    private readonly object _syncObj = new();
    private readonly IOptionsMonitor<KafkaFactoryOptions> _topicOptions;
    private bool _disposed;

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
        this._loggerFactory = loggerFactory;
        this._logger = loggerFactory?.CreateLogger("Epam.Kafka.Factory");
    }

    public void Dispose()
    {
        this._disposed = true;

        lock (this._syncObj)
        {
            foreach (KeyValuePair<KafkaClusterOptions, AdminClient> producer in this._clients)
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

        ValidateLogicalName(configName);

        try
        {
            KafkaConsumerOptions options = this._consumerOptions.Get(configName);

            return new ConsumerConfig(options.ConsumerConfig.ToDictionary(x => x.Key, x => x.Value));
        }
        catch (OptionsValidationException e)
        {
            throw new InvalidOperationException(
                $"Consumer config '{configName}' in corrupted state. See inner exception for details.", e);
        }
    }

    public ProducerConfig CreateProducerConfig(string? configName = null)
    {
        this.CheckIfDisposed();

        configName ??= this._topicOptions.CurrentValue.Producer;

        ValidateLogicalName(configName);

        try
        {
            KafkaProducerOptions options = this._producerOptions.Get(configName);

            return new ProducerConfig(options.ProducerConfig.ToDictionary(x => x.Key, x => x.Value));
        }
        catch (Exception e)
        {
            throw new InvalidOperationException(
                $"Producer config '{configName}' in corrupted state. See inner exception for details.", e);
        }
    }

    public IConsumer<TKey, TValue> CreateConsumer<TKey, TValue>(ConsumerConfig config, string? cluster = null,
        Action<ConsumerBuilder<TKey, TValue>>? configure = null)
    {
        this.CheckIfDisposed();

        KafkaClusterOptions clusterOptions = this.GetAndValidateClusterOptions(cluster);

        Dictionary<string, string> resultConfig = MergeResultConfig(clusterOptions, config);

        var builder = new ConsumerBuilder<TKey, TValue>(resultConfig);

        configure?.Invoke(builder);

        if (clusterOptions is { OauthHandler: { }, ClientConfig.SaslMechanism: SaslMechanism.OAuthBearer })
        {
            try
            {
                builder.SetOAuthBearerTokenRefreshHandler(clusterOptions.OauthHandler.Invoke);
            }
            catch (InvalidOperationException)
            {
            } // handler already set
        }

        if (this._loggerFactory != null)
        {
            ILogger logger = this._loggerFactory.CreateLogger("Epam.Kafka.DefaultLogHandler");

            try
            {
                builder.SetLogHandler((_, m) => logger.KafkaLogHandler(m));
            }
            catch (InvalidOperationException)
            {
            } // handler already set
        }

        try
        {
            IConsumer<TKey, TValue> consumer = builder.Build();

            this._logger?.ConsumerCreateOk(PrepareConfigForLogs(resultConfig), typeof(TKey), typeof(TValue));

            return consumer;
        }
        catch (Exception exc)
        {
            this._logger?.ConsumerCreateError(exc, PrepareConfigForLogs(resultConfig), typeof(TKey), typeof(TValue));

            throw;
        }
    }

    public IProducer<TKey, TValue> CreateProducer<TKey, TValue>(ProducerConfig config, string? cluster = null,
        Action<ProducerBuilder<TKey, TValue>>? configure = null)
    {
        this.CheckIfDisposed();

        KafkaClusterOptions clusterOptions = this.GetAndValidateClusterOptions(cluster);

        Dictionary<string, string> resultConfig = MergeResultConfig(clusterOptions, config);

        ProducerBuilder<TKey, TValue> builder = new(resultConfig);

        configure?.Invoke(builder);

        if (clusterOptions is { OauthHandler: { }, ClientConfig.SaslMechanism: SaslMechanism.OAuthBearer })
        {
            try
            {
                builder.SetOAuthBearerTokenRefreshHandler(clusterOptions.OauthHandler);
            }
            catch (InvalidOperationException)
            {
            } // handler already set
        }

        if (this._loggerFactory != null)
        {
            ILogger logger = this._loggerFactory.CreateLogger("Epam.Kafka.DefaultLogHandler");

            try
            {
                builder.SetLogHandler((_, m) => logger.KafkaLogHandler(m));
            }
            catch (InvalidOperationException)
            {
            } // handler already set
        }

        try
        {
            IProducer<TKey, TValue> producer = builder.Build();

            this._logger?.ProducerCreateOk(PrepareConfigForLogs(resultConfig), typeof(TKey), typeof(TValue));

            return producer;
        }
        catch (Exception exc)
        {
            this._logger?.ProducerCreateError(exc, PrepareConfigForLogs(resultConfig), typeof(TKey), typeof(TValue));

            throw;
        }
    }

    public IClient GetOrCreateClient(string? cluster = null)
    {
        this.CheckIfDisposed();

        KafkaClusterOptions clusterOptions = this.GetAndValidateClusterOptions(cluster);

        AdminClient? result;

        lock (this._syncObj)
        {
            if (!this._clients.TryGetValue(clusterOptions, out result))
            {
                var config = new ProducerConfig(clusterOptions.ClientConfig);

                result = new AdminClient(this.CreateProducer<Null, Null>(config, cluster));

                this._clients.Add(clusterOptions, result);
            }
        }

        return result;
    }

    public ISchemaRegistryClient GetOrCreateSchemaRegistryClient(string? cluster = null)
    {
        this.CheckIfDisposed();

        KafkaClusterOptions clusterOptions = this.GetAndValidateClusterOptions(cluster);

        CachedSchemaRegistryClient? result;

        lock (this._syncObj)
        {
            if (!this._registries.TryGetValue(clusterOptions, out result))
            {
                result = new CachedSchemaRegistryClient(clusterOptions.SchemaRegistryConfig,
                    clusterOptions.AuthenticationHeaderValueProvider);

                this._registries.Add(clusterOptions, result);
            }
        }

        return result;
    }

    private static void ValidateLogicalName(string? configName)
    {
        if (string.IsNullOrWhiteSpace(configName))
        {
            throw new InvalidOperationException("Unable to create entity with null or whitespace logical name.");
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> PrepareConfigForLogs(
        Dictionary<string, string> resultConfig)
    {
        return resultConfig.Select(x => Contains(x, "password") || Contains(x, "secret")
            ? new KeyValuePair<string, string>(x.Key, "*******")
            : x);

        static bool Contains(KeyValuePair<string, string> x, string value)
        {
            return x.Key.Contains(value
#if NET6_0_OR_GREATER
                , StringComparison.Ordinal
#endif
            );
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

        ValidateLogicalName(cluster);

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