// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka;
using Epam.Kafka.PubSub;
using Epam.Kafka.PubSub.EntityFrameworkCore;
using Epam.Kafka.PubSub.EntityFrameworkCore.Subscription.State;
using Epam.Kafka.PubSub.Subscription;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SubscribeEfCoreOffsets;

internal class Program
{
    static void Main(string[] args)
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

        hostBuilder.ConfigureServices((context, services) =>
        {
            // register db context for offsets storage
            services.AddDbContext<OffsetsDbContext>(x => x.UseInMemoryDatabase("Sample"));
            // and configure default external offsets storage implementation
            services.TryAddKafkaDbContextState<OffsetsDbContext>();

            KafkaBuilder builder = services.AddKafka();

            // use mock cluster for demo purposes only, NOT for production!
            builder.WithTestMockCluster("Sandbox");

            // configure custom placeholders that can be used in config
            builder.WithConfigPlaceholders($"<{nameof(context.HostingEnvironment.EnvironmentName)}>", context.HostingEnvironment.EnvironmentName);

            // subscription summary health check with "live" tag
            builder.WithSubscriptionSummaryHealthCheck(new[] { "live" });

            // add subscription with default offsets storage managed by kafka broker
            builder.AddSubscription<Ignore, Ignore, SubSample>(nameof(SubSample))
                .WithHealthChecks()

                // store offsets in external storage (registered above) instead of Kafka internal storage, however keep using consumer group re-balancing
                // use .WithAssignAndExternalOffsets() to assign constant topic partitions instead of group re-balancing
                .WithSubscribeAndExternalOffsets()
                // seed topic before processing
                .WaitFor(sp =>
                {
                     sp.GetRequiredKeyedService<TestMockCluster>("Sandbox").SeedTopic("sample.name",
                            new Message<byte[], byte[]?> { Key = Guid.NewGuid().ToByteArray() });

                     return Task.CompletedTask;
                });

        }).Build().Run();
    }
}

public class SubSample : ISubscriptionHandler<Ignore, Ignore>
{
    private readonly ILogger<SubSample> _logger;

    public SubSample(ILogger<SubSample> logger)
    {
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    public void Execute(IReadOnlyCollection<ConsumeResult<Ignore, Ignore>> items, CancellationToken cancellationToken)
    {
        foreach (ConsumeResult<Ignore, Ignore> result in items)
        {
            this._logger.LogInformation("Processed {Tpo}", result.TopicPartitionOffset);
        }
    }
}

public class OffsetsDbContext : DbContext, IKafkaStateDbContext
{
    public OffsetsDbContext(DbContextOptions<OffsetsDbContext> options) : base(options)
    {
    }

    public DbSet<KafkaTopicState> KafkaTopicStates => this.Set<KafkaTopicState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddKafkaState();
    }
}