// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace Epam.Kafka.Tests.Common;

public sealed class MockCluster
{
    public const string ClusterName = "Mock";
    public const string TransactionalProducer = "Transactional";
    public const string DefaultConsumer = "Default";
    public const string DefaultConsumerGroup = "consumer.epam-kafka-tests";

    private const string DefaultProducer = "Default";

    private readonly string _mockBootstrapServers = "localhost:9092";

    public KafkaBuilder LaunchMockCluster(TestWithServices test)
    {
        return AddMockCluster(test, this._mockBootstrapServers);
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
            });
        }

        kafkaBuilder.WithProducerConfig(DefaultProducer);

        return kafkaBuilder;
    }

    public static async Task SeedKafka(TestWithServices test)
    {
        try
        {
            await test.KafkaFactory.GetOrCreateClient().CreateDependentAdminClient().CreateTopicsAsync(new[]
            {
                new TopicSpecification
                {
                    Name = test.AnyTopicName,
                    NumPartitions = 4,
                    ReplicationFactor = 1
                }
            });
        }
        catch (CreateTopicsException)
        {
        }
    }

    public static async Task<Dictionary<TestEntityKafka, TopicPartitionOffset>> SeedKafka(TestWithServices test,
        int count, TopicPartition tp)
    {
        await SeedKafka(test);

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

        if (result.Count < count)
        {
            await Task.Run(() => producer.Poll(TimeSpan.FromSeconds(5)));
        }

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