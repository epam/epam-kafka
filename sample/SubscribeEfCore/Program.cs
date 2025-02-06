// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka;
using Epam.Kafka.PubSub;
using Epam.Kafka.PubSub.EntityFrameworkCore;
using Epam.Kafka.PubSub.EntityFrameworkCore.Subscription;
using Epam.Kafka.PubSub.EntityFrameworkCore.Subscription.State;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using System.Text.Json;

namespace SubscribeEfCore;

internal class Program
{
    private static readonly string[] Tags = new[] { "live" };

    private static void Main(string[] args)
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

        hostBuilder.ConfigureServices((context, services) =>
        {
            // register db context for offsets storage
            services.AddDbContext<SampleDbContext>(x => x.UseInMemoryDatabase("Sample"));
            // and configure default external offsets storage implementation
            services.TryAddKafkaDbContextState<SampleDbContext>();

            KafkaBuilder builder = services.AddKafka();

            // use mock cluster for demo purposes only, NOT for production!
            builder.WithTestMockCluster("Sandbox");

            // configure custom placeholders that can be used in config
            builder.WithConfigPlaceholders($"<{nameof(context.HostingEnvironment.EnvironmentName)}>", context.HostingEnvironment.EnvironmentName);

            // subscription summary health check with "live" tag
            builder.WithSubscriptionSummaryHealthCheck(Tags);

            // add subscription with default offsets storage managed by kafka broker
            builder.AddSubscription<long, SampleKafkaEntity?, SubSample>(nameof(SubSample))
                .WithHealthChecks()
                .WithValueDeserializer(sr => new SampleKafkaEntitySerializer())
                // store offsets in external storage (registered above) instead of Kafka internal storage.
                // To keep using consumer group re-balancing use .WithSubscribeAndExternalOffsets() instead.
                .WithAssignAndExternalOffsets()
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

public class SampleDbEntity
{
    public long Id { get; init; }
    public string? Name { get; set; }
    public int Partition { get; set; }
    public long Offset { get; set; }
}

public class SampleKafkaEntity
{
    public long Id { get; set; }

    public required string Name { get; set; }
}

public class SampleKafkaEntitySerializer : ISerializer<SampleKafkaEntity>, IDeserializer<SampleKafkaEntity?>
{
    public byte[] Serialize(SampleKafkaEntity data, SerializationContext context)
    {
        return JsonSerializer.SerializeToUtf8Bytes(data);
    }

    public SampleKafkaEntity? Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return isNull ? null : JsonSerializer.Deserialize<SampleKafkaEntity>(data);
    }
}

public class SampleDbContext : DbContext, IKafkaStateDbContext
{
    public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options)
    {
    }

    public DbSet<SampleDbEntity> Entities => this.Set<SampleDbEntity>();

    public DbSet<KafkaTopicState> KafkaTopicStates => this.Set<KafkaTopicState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddKafkaState();
    }
}

public class SubSample : DbContextEntitySubscriptionHandler<long, SampleKafkaEntity?, SampleDbContext, SampleDbEntity>
{

    public SubSample(SampleDbContext context, ILogger<SubSample> logger) : base(context, logger)
    {
    }

    protected override bool IsDeleted(ConsumeResult<long, SampleKafkaEntity?> value)
    {
        // thumbstone record
        return value.Message.Value == null;
    }

    protected override void LoadMainChunk(IQueryable<SampleDbEntity> queryable, IReadOnlyCollection<ConsumeResult<long, SampleKafkaEntity?>> chunk)
    {
        long[] ids = chunk.Select(x => x.Message.Key).ToArray();

        // load required entities from DB for further processing
        queryable.Where(x => ids.Contains(x.Id)).Load();
    }

    protected override SampleDbEntity? FindLocal(DbSet<SampleDbEntity> dbSet, ConsumeResult<long, SampleKafkaEntity?> value)
    {
        // try to find existing DB entity by id
        return dbSet.Find(value.Message.Key);
    }

    protected override string? Update(ConsumeResult<long, SampleKafkaEntity?> value, SampleDbEntity entity, bool created)
    {
        SampleKafkaEntity? v = value.Message.Value;

        if (v != null)
        {
            entity.Name = v.Name;
            entity.Partition = value.Partition;
            entity.Offset = value.Offset;
        }

        return created ? "Created" : "Updated";
    }

    protected override bool TryCreate(ConsumeResult<long, SampleKafkaEntity?> value, out SampleDbEntity? entity)
    {
        entity = null;

        if (value.Message.Value != null)
        {
            entity = new SampleDbEntity { Id = value.Message.Key };
        }

        return entity != null;
    }

    protected override void SaveChanges()
    {
        // optionally don't save changes in handler
        // because offsets and data stored in same DB context
        // so that data will be committed with offsets in single DB transaction
    }
}