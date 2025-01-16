// Copyright © 2024 EPAM Systems

using System.Text.Json;
using Confluent.Kafka;
using Epam.Kafka;
using Epam.Kafka.PubSub;
using Epam.Kafka.PubSub.EntityFrameworkCore.Publication.Contracts;
using Epam.Kafka.PubSub.Publication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PublishTransactionEfCore;

internal class Program
{
    static void Main(string[] args)
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

        hostBuilder.ConfigureServices((context, services) =>
        {
            services.AddDbContext<SampleDbContext>(x => x.UseInMemoryDatabase("sample"));

            KafkaBuilder builder = services.AddKafka();

            // use mock cluster for demo purposes only, NOT for production!
            builder.WithTestMockCluster("Sandbox");

            // configure custom placeholders that can be used in config
            builder.WithConfigPlaceholders($"<{nameof(context.HostingEnvironment.EnvironmentName)}>", context.HostingEnvironment.EnvironmentName);

            // publication summary health check with "live" tag
            builder.WithPublicationSummaryHealthCheck(new[] { "live" });

            // Read data from DB using Entity framework core,
            // convert to multiple messages,
            // publish messages to kafka in single transaction,
            // and finally update row state in database
            builder.AddPublication<long, SampleKafkaEntity, PubSample>(nameof(PubSample))
                .WithHealthChecks()
                .WithValueSerializer(sr => new SampleKafkaEntitySerializer())
                // seed database before processing
                .WaitFor(sp =>
                {
                    SampleDbContext db = sp.GetRequiredService<SampleDbContext>();

                    db.Set<SampleDbEntity>().Add(new SampleDbEntity { Id = 1, Name = "qwe", KafkaPubState = KafkaPublicationState.Queued });

                    return db.SaveChangesAsync();
                });

        }).Build().Run();
    }
}

public class SampleDbEntity : IKafkaPublicationEntity
{
    public int Id { get; set; }

    public string Name { get; set; }

    public KafkaPublicationState KafkaPubState { get; set; }

    public DateTime KafkaPubNbf { get; set; }
}

public class SampleKafkaEntity
{
    public long Id { get; set; }

    public string Name { get; set; }
}

public class SampleKafkaEntitySerializer : ISerializer<SampleKafkaEntity>
{
    public byte[] Serialize(SampleKafkaEntity data, SerializationContext context)
    {
        return JsonSerializer.SerializeToUtf8Bytes(data);
    }
}

public class SampleDbContext : DbContext
{
    public SampleDbContext(DbContextOptions<SampleDbContext> options) : base(options)
    {

    }

    public DbSet<SampleDbEntity> Entities { get; } = null!;
}

public class PubSample : DbContextEntityPublicationHandler<long, SampleKafkaEntity, SampleDbEntity, SampleDbContext>
{
    public PubSample(SampleDbContext context, ILogger<PubSample> logger) : base(context, logger)
    {
    }

    protected override IEnumerable<TopicMessage<long, SampleKafkaEntity>> Convert(SampleDbEntity dbEntity)
    {
        // all messages will be published in single kafka transaction
        // due to producer configuration in appsettings.json
        yield return new TopicMessage<long, SampleKafkaEntity>
        {
            Key = dbEntity.Id,
            Value = new SampleKafkaEntity { Id = dbEntity.Id, Name = dbEntity.Name }
        };

        yield return new TopicMessage<long, SampleKafkaEntity>
        {
            Key = dbEntity.Id,
            Value = new SampleKafkaEntity { Id = dbEntity.Id, Name = dbEntity.Name },
            Topic = "another.topic"
        };
    }

    protected override IOrderedQueryable<SampleDbEntity> OrderBy(IQueryable<SampleDbEntity> query)
    {
        return query.OrderBy(x => x.Id);
    }
}