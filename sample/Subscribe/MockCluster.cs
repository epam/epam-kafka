// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Subscribe;

public sealed class MockCluster : IDisposable
{
    private readonly Lazy<IProducer<byte[], byte[]?>> _producerLazy;

    private readonly ILogger _logger;

    public MockCluster(int numBrokers, ILoggerFactory? loggerFactory = null)
    {
        if (numBrokers <= 0) throw new ArgumentOutOfRangeException(nameof(numBrokers));
        if (numBrokers > 3) throw new ArgumentOutOfRangeException(nameof(numBrokers));

        this._logger = loggerFactory?.CreateLogger(typeof(MockCluster)) ?? NullLogger.Instance;

        this._producerLazy =
            new(() =>
            {
                ProducerConfig config = new ProducerConfig
                {
                    AllowAutoCreateTopics = true,
                    BootstrapServers = "localhost:9200"
                };

                config.Set("test.mock.num.brokers", $"{numBrokers}");

                ProducerBuilder<byte[], byte[]?> pb = new(config);

                pb.SetLogHandler((_, message) => this._logger.KafkaLogHandler(message));

                return pb.Build();
            }, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string BootstrapServers
    {
        get
        {
            using IAdminClient adminClient = this._producerLazy.Value.CreateDependentAdminClient();

            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(1));

            return string.Join(", ", metadata.Brokers.Select(x => $"{x.Host}:{x.Port}"));
        }
    }

    public async Task<DeliveryResult<byte[], byte[]?>> SeedTopicAsync(string topicName, byte[] key, byte[]? value, Headers? headers = null)
    {
        Message<byte[], byte[]?> message = new Message<byte[], byte[]?>
        {
            Key = key,
            Value = value,
            Headers = headers
        };

        DeliveryResult<byte[], byte[]?> result = await this._producerLazy.Value.ProduceAsync(topicName, message);

        return result;
    }

    void IDisposable.Dispose()
    {
        if (this._producerLazy.IsValueCreated)
        {
            this._producerLazy.Value.Dispose();
        }
    }
}