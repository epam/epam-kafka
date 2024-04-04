// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub;
using Epam.Kafka.PubSub.Common.Pipeline;
using Epam.Kafka.PubSub.EntityFrameworkCore;
using Epam.Kafka.PubSub.EntityFrameworkCore.Publication.Contracts;
using Epam.Kafka.Sample.Data;
using Epam.Kafka.Sample.Json;
using Epam.Kafka.Sample.Samples;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

using OpenTelemetry.Metrics;

namespace Epam.Kafka.Sample;

internal static class Program
{
    private static void Main(string[] args)
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

        hostBuilder.ConfigureServices(services =>
        {


            KafkaBuilder kafkaBuilder = services.AddKafka();
            //kafkaBuilder.WithProducerConfig("Default").Configure(x => x.)


            kafkaBuilder.WithClusterConfig("Sandbox").Configure(options =>
            {
                // For demo purposes run Mock server instead of real one and create required topics.
                // Also it is possible to run real kafka cluster in docker using provided 'docker-compose.yml' file.
                options.ClientConfig.BootstrapServers = "localhost:9092";
                options.ClientConfig.Debug = "all";
                options.ClientConfig.AllowAutoCreateTopics = true;
            });


            // Sample of IKafkaFactory usage 
            services.AddHostedService<ProducerSample>();

        }).Build().Run();
    }

    private static string RunMockServer()
    {
        var clientConfig = new ClientConfig();
        clientConfig.Set("bootstrap.servers", "localhost:9200");
        clientConfig.Set("test.mock.num.brokers", "1");
        clientConfig.Set("allow.auto.create.topics", "true");

        IAdminClient adminClient = new AdminClientBuilder(clientConfig).Build();

        Metadata metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(1));

        return string.Join(",", metadata.Brokers.Select(b => $"{b.Host}:{b.Port}"));
    }

    private static async Task SeedTopic(IServiceProvider sp)
    {
        IKafkaFactory factory = sp.GetRequiredService<IKafkaFactory>();

        using IProducer<string, KafkaEntity> producer = factory
            .GetOrCreateClient("Sandbox")
            .CreateDependentProducer<string, KafkaEntity>(builder =>
            {
                builder.SetValueSerializer(Utf8JsonSerializer.Instance);
            });

        KafkaEntity entity = new() { Id = "qwe" };

        await producer.ProduceAsync("epam-kafka-sample-topic-2",
            new Message<string, KafkaEntity> { Key = entity.Id, Value = entity });
    }

    private static async Task SeedDatabase(IServiceProvider sp)
    {
        await using SampleDbContext context = sp.GetRequiredService<SampleDbContext>();

        context.Add(new SamplePublicationEntity { Id = "qwe", KafkaPubState = KafkaPublicationState.Queued });

        await context.SaveChangesAsync();
    }
}