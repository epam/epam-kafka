// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka;
using Epam.Kafka.PubSub;
using Epam.Kafka.PubSub.Publication;
using Epam.Kafka.PubSub.Subscription.Replication;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System.Text;
using System.Text.Json;

namespace SubscribeReplicationTransactional;

internal class Program
{
    private static readonly string[] Tags = new[] { "live" };

    private static void Main(string[] args)
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

        hostBuilder.ConfigureServices((context, services) =>
        {
            KafkaBuilder builder = services.AddKafka();

            // use mock cluster for demo purposes only, NOT for production!
            builder.WithTestMockCluster("Sandbox");

            // configure custom placeholders that can be used in config
            builder.WithConfigPlaceholders($"<{nameof(context.HostingEnvironment.EnvironmentName)}>", context.HostingEnvironment.EnvironmentName);

            // subscription summary health check with "live" tag
            builder.WithSubscriptionSummaryHealthCheck(Tags);

            // add subscription with default offsets storage managed by kafka broker
            builder.AddReplication<long, SampleKafkaEntity?, long, SampleKafkaEntity?, SubSample>(nameof(SubSample), valueSerializer: sr => new SampleKafkaEntitySerializer())
                .WithHealthChecks()

                .WithValueDeserializer(sr => new SampleKafkaEntitySerializer())
                // store offsets in external storage (registered above) instead of Kafka internal storage.
                // To keep using consumer group re-balancing use .WithSubscribeAndExternalOffsets() instead.
                // seed topic before processing
                .WaitFor(sp =>
                {
                    SampleKafkaEntitySerializer vs = new();
                    var e1 = new SampleKafkaEntity { Id = 1, Name = "N1" };
                    var e2 = new SampleKafkaEntity { Id = 1, Name = "N2" };

                    sp.GetRequiredKeyedService<TestMockCluster>("Sandbox").SeedTopic("sample.name",
                        new Message<byte[], byte[]?> { Key = Serializers.Int64.Serialize(e1.Id, SerializationContext.Empty), Value = vs.Serialize(e1, SerializationContext.Empty) },
                        new Message<byte[], byte[]?> { Key = Serializers.Int64.Serialize(e2.Id, SerializationContext.Empty), Value = vs.Serialize(e1, SerializationContext.Empty) },
                        new Message<byte[], byte[]?> { Key = Serializers.Int64.Serialize(e1.Id, SerializationContext.Empty), Value = null });

                    return Task.CompletedTask;
                });

        }).Build().Run();
    }
}

public class SampleKafkaEntity
{
    public long Id { get; set; }

    public required string Name { get; set; }
}

public class SampleKafkaEntitySerializer : ISerializer<SampleKafkaEntity?>, IDeserializer<SampleKafkaEntity?>
{
    public byte[]? Serialize(SampleKafkaEntity? data, SerializationContext context)
    {
        return data != null ? JsonSerializer.SerializeToUtf8Bytes(data) : null;
    }

    public SampleKafkaEntity? Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return isNull ? null : JsonSerializer.Deserialize<SampleKafkaEntity>(data);
    }
}

public class SubSample : ConvertHandler<long, SampleKafkaEntity?, ConsumeResult<long, SampleKafkaEntity?>>
{
    protected override IEnumerable<TopicMessage<long, SampleKafkaEntity?>> ConvertSingle(ConsumeResult<long, SampleKafkaEntity?> entity, CancellationToken cancellationToken)
    {
        yield return new TopicMessage<long, SampleKafkaEntity?>
        {
            Key = entity.Message.Key,
            Value = entity.Message.Value,
            Headers = new Headers
            {
                { "x-from", Encoding.UTF8.GetBytes(entity.TopicPartitionOffset.ToString()) }
            }
        };
    }
}