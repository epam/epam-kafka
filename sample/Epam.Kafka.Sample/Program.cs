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
            // view health check results in console for demo purposes only.
            services.AddSingleton<IHealthCheckPublisher, ConsoleHealthCheckPublisher>();

            // view metrics in console for demo purposes only
            services.AddOpenTelemetry().WithMetrics(mb =>
                mb.AddMeter(PipelineMonitor.StatusMeterName, PipelineMonitor.HealthMeterName).AddConsoleExporter());

            string domainName = AppDomain.CurrentDomain.FriendlyName;
            string machineName = Environment.MachineName;

            KafkaBuilder kafkaBuilder = services.AddKafka();
            
            kafkaBuilder.WithPubSubSummaryHealthCheck();

            kafkaBuilder.WithClusterConfig("Sandbox").Configure(options =>
            {
                // For demo purposes run Mock server instead of real one and create required topics.
                // Also it is possible to run real kafka cluster in docker using provided 'docker-compose.yml' file.
                options.ClientConfig.BootstrapServers = RunMockServer();
            });

            services.AddDbContext<SampleDbContext>(x => x.UseInMemoryDatabase("sample"));
            services.TryAddKafkaDbContextState<SampleDbContext>();

            // Sample of IKafkaFactory usage 
            services.AddHostedService<ProducerSample>();
            services.AddHostedService<ConsumerSample>();

            // Sample of subscription that read data from kafka and store data to database using EF Core.
            kafkaBuilder.AddSubscription<string, KafkaEntity?, SubscriptionHandlerSample>("Sample")
                .WithSubscribeAndExternalOffsets()
                .WithValueDeserializer(_ => Utf8JsonSerializer.Instance)
                .WithHealthChecks()
                .WaitFor(SeedTopic)

                // optionally set subscription specific client id to consumer
                .WithConsumerConfigModification(c =>
                {
                    c.ClientId = $"{domainName}@{machineName}:SubSample";
                });

            // Sample of publication that read data from database using EF Core and write to kafka.
            kafkaBuilder.AddPublication<string, KafkaEntity, PublicationHandlerSample>("Sample")
                .WithValueSerializer(_ => Utf8JsonSerializer.Instance)
                .WithHealthChecks()
                .WaitFor(SeedDatabase)

                // optionally set publication specific client id and transaction id to producer. 
                .WithProducerConfigModification(c =>
                {
                    c.ClientId = $"{domainName}@{machineName}:PubSample";

                    if (!string.IsNullOrEmpty(c.TransactionalId))
                    {
                        c.TransactionalId = $"{c.TransactionalId}.sample";
                    }
                });
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