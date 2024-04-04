// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Microsoft.Extensions.Configuration;
using Shouldly;

namespace Epam.Kafka.Tests.Common;

public sealed class MockCluster : IDisposable
{
    public const string ClusterName = "Mock";
    public const string TransactionalProducer = "Transactional";
    public const string DefaultConsumer = "Default";
    public const string DefaultConsumerGroup = "consumer.epam-kafka-tests";

    private const string DefaultProducer = "Default";

    private readonly IAdminClient? _adminClient;
    private readonly string _mockBootstrapServers;

    public MockCluster()
    {
        string? env = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS");

        if (env != null)
        {
            this._mockBootstrapServers = env;
        }
        else
        {
            // Trick: we are going to connect with admin client first so we can get metadata and bootstrap servers from it
            var clientConfig = new ClientConfig();
            clientConfig.Set("bootstrap.servers", "localhost:9200");
            clientConfig.Set("test.mock.num.brokers", "1");
            this._adminClient = new AdminClientBuilder(clientConfig).Build();
            Metadata metadata = this._adminClient.GetMetadata(TimeSpan.FromSeconds(1));
            this._mockBootstrapServers = string.Join(",", metadata.Brokers.Select(b => $"{b.Host}:{b.Port}"));
        }
    }

    public KafkaBuilder LaunchMockCluster(TestWithServices test)
    {
        Console.WriteLine($"this._mockBootstrapServers: {this._mockBootstrapServers}");
        test.Output.WriteLine($"this._mockBootstrapServers: {this._mockBootstrapServers}");
        return AddMockCluster(test, this._mockBootstrapServers);
    }

    public void Dispose()
    {
        this._adminClient?.Dispose();
    }

    public static KafkaBuilder AddMockCluster(TestWithServices test, string? server = null)
    {
        test.ConfigurationBuilder.AddInMemoryCollection(GetDefaultFactoryConfig());

        KafkaBuilder kafkaBuilder = test.Services.AddKafka();
        
        if (server != null)
        {
            kafkaBuilder.WithClusterConfig(ClusterName).Configure(options =>
            {
                options.ClientConfig.AllowAutoCreateTopics = true;
                options.ClientConfig.BootstrapServers = server;
                options.ClientConfig.Debug = "all";
            });
        }

        kafkaBuilder.WithProducerConfig(DefaultProducer);

        return kafkaBuilder;
    }

    public static async Task<Dictionary<TestEntityKafka, TopicPartitionOffset>> SeedKafka(TestWithServices test,
        int count, TopicPartition tp)
    {
        ProducerConfig config = test.KafkaFactory.CreateProducerConfig();

        config.EnableIdempotence = true;
        config.EnableBackgroundPoll = false;
        config.Acks = Acks.All;
        config.EnableDeliveryReports = true;

        using IProducer<string, byte[]> producer = test.KafkaFactory.CreateProducer<string, byte[]>(config, ClusterName);

        Dictionary<TestEntityKafka, TopicPartitionOffset> result = new(count);

        for (int i = 0; i < count; i++)
        {
            TestEntityKafka entity = new();

            producer.Produce(tp, entity.ToBytesMessage(), r => result.Add(entity, r.TopicPartitionOffset));
        }

        await Task.Run(() => producer.Poll(TimeSpan.FromSeconds(2)));

        result.Count.ShouldBe(count, $"Seed {count} items.");

        return result;
    }

    private static Dictionary<string, string?> GetDefaultFactoryConfig()
    {
        return new Dictionary<string, string?>
        {
            { "Kafka:Default:Cluster", ClusterName },
            { "Kafka:Default:Consumer", DefaultConsumer },
            { "Kafka:Default:Producer", DefaultProducer },
            { $"Kafka:Clusters:{ClusterName}:bootstrap.servers", "localhost:9092" },
            { $"Kafka:Clusters:{ClusterName}:schema.registry.url", "http://localhost:9092" },
            { $"Kafka:Producers:{TransactionalProducer}:transactional.id", "producer.epam-kafka-tests" },
            { $"Kafka:Consumers:{DefaultConsumer}:group.id", DefaultConsumerGroup },

            { "Kafka:Clusters:b1:bootstrap.servers", "localhost:9091" },
            { "Kafka:Clusters:b1:schema.registry.url", "http://localhost:9091" },
            { "Kafka:Clusters:b2:bootstrap.servers", "localhost:9092" },
            { "Kafka:Clusters:b2:schema.registry.url", "http://localhost:9092" },
            { "Kafka:Consumers:c1:group.id", "g1" },
            { "Kafka:Consumers:c2:group.id", "g2" },
            { "Kafka:Producers:p1:transactional.id", "t1" },
            { "Kafka:Producers:p2:transactional.id", "t2" },
        };
    }
}