// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Epam.Kafka;

/// <summary>
/// EXPERIMENTAL API, SUBJECT TO CHANGE OR REMOVAL
/// Provides mock Kafka cluster with a configurable number of brokers
/// that support a reasonable subset of Kafka protocol operations,
/// error injection, etc. Mock clusters provide localhost listeners that can be used as the bootstrap
/// servers.
/// </summary>
/// <remarks>
/// Currently supported functionality:
/// <list type="string">Producer</list>
/// <list type="string">Idempotent Producer</list>
/// <list type="string">Transactional Producer</list>
/// <list type="string">Low-level consumer</list>
/// <list type="string">High-level balanced consumer groups with offset commits</list>
/// <list type="string">Topic Metadata and auto creation</list>
/// <list type="string">Telemetry (KIP-714)</list>
/// </remarks>
public sealed class TestMockCluster : IDisposable
{
    private readonly Lazy<IProducer<byte[], byte[]?>> _producerLazy;

    private const int DefaultTimeoutMs = 5000;

    private bool _disposed;

    /// <summary>
    /// Initialize the <see cref="TestMockCluster" /> instance.
    /// </summary>
    /// <param name="numBrokers">The number of brokers (default 1)</param>
    /// <param name="loggerFactory">Optional logger factory</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="numBrokers"/> should be in range [1,3].</exception>
    public TestMockCluster(int numBrokers = 1, ILoggerFactory? loggerFactory = null)
    {
        if (numBrokers <= 0) throw new ArgumentOutOfRangeException(nameof(numBrokers));
        if (numBrokers > 3) throw new ArgumentOutOfRangeException(nameof(numBrokers));

        ILogger logger = loggerFactory?.CreateLogger(typeof(TestMockCluster)) ?? NullLogger.Instance;

        this._producerLazy =
            new(() =>
            {
                var config = new ProducerConfig
                {
                    AllowAutoCreateTopics = true,
                    BootstrapServers = "localhost:9200",
                    MessageTimeoutMs = DefaultTimeoutMs
                };

                config.Set("test.mock.num.brokers", $"{numBrokers}");

                ProducerBuilder<byte[], byte[]?> pb = new(config);

                pb.SetLogHandler((_, message) => logger.KafkaLogHandler(message));

                return pb.Build();
            }, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>
    /// Comma separated addresses of localhost listeners that can be used as the bootstrap
    /// servers
    /// </summary>
    public string BootstrapServers
    {
        get
        {
            if (this._disposed)
            {
                throw new ObjectDisposedException(nameof(TestMockCluster));
            }

            using IAdminClient adminClient = this._producerLazy.Value.CreateDependentAdminClient();

            Metadata metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(1));

            return string.Join(", ", metadata.Brokers.Select(x => $"{x.Host}:{x.Port}"));
        }
    }

    /// <summary>
    /// Produce messages to desired topic. If topic not exists it will be created even if no messages provided.
    /// </summary>
    /// <param name="topicName">The topic name.</param>
    /// <param name="messages">Messages to produce</param>
    /// <returns>The <see cref="DeliveryResult{TKey,TValue}"/>.</returns>
    public Dictionary<Message<byte[], byte[]?>, DeliveryResult<byte[], byte[]?>> SeedTopic(string topicName, params Message<byte[], byte[]?>[] messages)
    {
        if (topicName == null) throw new ArgumentNullException(nameof(topicName));
        if (messages == null) throw new ArgumentNullException(nameof(messages));

        if (this._disposed)
        {
            throw new ObjectDisposedException(nameof(TestMockCluster));
        }

        IProducer<byte[], byte[]?> producer = this._producerLazy.Value;

        Dictionary<Message<byte[], byte[]?>, DeliveryResult<byte[], byte[]?>> result = new(messages.Length);

        if (messages.Length > 0)
        {
            foreach (Message<byte[], byte[]?> m in messages)
            {
                producer.Produce(topicName, m, r => result[m] = r);
            }

            DateTime wait = DateTime.UtcNow.AddMilliseconds(DefaultTimeoutMs);

            while (DateTime.UtcNow < wait && result.Count < messages.Length)
            {
                producer.Poll(TimeSpan.FromMilliseconds(DefaultTimeoutMs / 10));
            }

            if (result.Count != messages.Length)
            {
                throw new InvalidOperationException($"Produced {result.Count} of {messages.Length} messages.");
            }
        }
        else
        {
            using IAdminClient adminClient = producer.CreateDependentAdminClient();

            adminClient.GetMetadata(topicName, TimeSpan.FromMilliseconds(DefaultTimeoutMs));
        }

        return result;
    }

    /// <summary>
    /// Creates depended admin client
    /// </summary>
    /// <returns></returns>
    public IAdminClient CreateDependentAdminClient()
    {
        if (this._disposed)
        {
            throw new ObjectDisposedException(nameof(TestMockCluster));
        }

        return this._producerLazy.Value.CreateDependentAdminClient();
    }

    /// <summary>
    /// Dispose mock cluster listener
    /// </summary>
    public void Dispose()
    {
        if (!this._disposed && this._producerLazy.IsValueCreated)
        {
            this._producerLazy.Value.Dispose();
        }

        this._disposed = true;
    }
}