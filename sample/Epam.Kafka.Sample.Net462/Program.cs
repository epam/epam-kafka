
using Confluent.Kafka;

using Microsoft.Extensions.Hosting;

using System.Threading.Tasks;

using System;
using System.Linq;
using Effort.Provider;
using Epam.Kafka.PubSub;
using Epam.Kafka.PubSub.EntityFramework6;
using Epam.Kafka.PubSub.EntityFramework6.Publication.Contracts;
using Epam.Kafka.Sample.Net462.Data;
using Epam.Kafka.Sample.Net462.Json;
using Microsoft.Extensions.DependencyInjection;
using Epam.Kafka.Sample.Net462.Samples;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Epam.Kafka.Sample.Net462
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Host.CreateDefaultBuilder().ConfigureServices(services =>
            {
                KafkaBuilder kafkaBuilder = services.AddKafka();

                kafkaBuilder.WithClusterConfig("Sandbox").Configure(options =>
                {
                    // For demo purposes run Mock server instead of real one and create required topics.
                    // Also it is possible to run real kafka cluster in docker using provided 'docker-compose.yml' file.
                    options.ClientConfig.BootstrapServers = RunMockServer();
                });

                // Sample of IKafkaFactory usage 
                services.AddHostedService<ProducerSample>();
                services.AddHostedService<ConsumerSample>();

                // add in memory db context and configure it to store offsets
                services.TryAddSingleton(Effort.DbConnectionFactory.CreatePersistent(Guid.NewGuid().ToString("N")));
                services.TryAddScoped(sp => new SampleDbContext(sp.GetRequiredService<EffortConnection>()));
                services.TryAddKafkaDbContextState<SampleDbContext>();

                // Sample of subscription that read data from kafka and store data to database using EF Core.
                kafkaBuilder.AddSubscription<string, KafkaEntity, SubscriptionHandlerSample>("Sample")
                    .WithSubscribeAndExternalOffsets()
                    .WithValueDeserializer(_ => Utf8JsonSerializer.Instance)
                    .WaitFor(SeedTopic);

                // Sample of publication that read data from database using EF Core and write to kafka.
                kafkaBuilder.AddPublication<string, KafkaEntity, PublicationHandlerSample>("Sample")
                    .WithValueSerializer(_ => Utf8JsonSerializer.Instance)
                    .WaitFor(SeedDatabase);

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

            using (IProducer<string, KafkaEntity> producer = factory
                       .GetOrCreateClient("Sandbox")
                       .CreateDependentProducer<string, KafkaEntity>(builder =>
                       {
                           builder.SetValueSerializer(Utf8JsonSerializer.Instance);
                       }))
            {

                KafkaEntity entity = new KafkaEntity() { Id = "qwe" };

                await producer.ProduceAsync("epam-kafka-sample-topic-2",
                    new Message<string, KafkaEntity> { Key = entity.Id, Value = entity });
            }
        }

        private static async Task SeedDatabase(IServiceProvider sp)
        {
            using (SampleDbContext context = sp.GetRequiredService<SampleDbContext>())
            {
                context.Set<SamplePublicationEntity>().Add(new SamplePublicationEntity { Id = "qwe", KafkaPubState = KafkaPublicationState.Queued });

                await context.SaveChangesAsync();
            }
        }
    }
}
