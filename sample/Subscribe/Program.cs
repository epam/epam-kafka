// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka;
using Epam.Kafka.PubSub;
using Epam.Kafka.PubSub.Subscription;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Subscribe;

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
            builder.AddSubscription<Ignore, Ignore, SubSample>(nameof(SubSample))
                .WithHealthChecks()

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