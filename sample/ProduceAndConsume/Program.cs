// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Epam.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ProduceAndConsume;

internal class Program
{
    static void Main(string[] args)
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

        hostBuilder.ConfigureServices(services =>
        {
            KafkaBuilder kafkaBuilder = services.AddKafka();

            // use mock cluster for demo purposes only, NOT for production!
            kafkaBuilder.WithTestMockCluster("Sandbox");

            // optionally append or override cluster settings configured in appsettings.json
            kafkaBuilder.WithClusterConfig("Sandbox").Configure(x =>
            {
                x.ClientConfig.AllowAutoCreateTopics = true;
                x.SchemaRegistryConfig.Url = "http://localhost:8080";
            });

            // optionally append or override consumer settings configured in appsettings.json
            kafkaBuilder.WithConsumerConfig("c1").Configure(x =>
            {
                x.ConsumerConfig.GroupId = "test1";
            });

            // optionally append or override producer settings configured in appsettings.json
            kafkaBuilder.WithProducerConfig("p1").Configure(x =>
            {
                x.ProducerConfig.EnableDeliveryReports = true;
            });

            // optionally configure default names for producer, consumer, and cluster configs
            // so that you don't need to provide their names to IKafkaFactory methods
            kafkaBuilder.WithDefaults(x =>
            {
                x.Producer = "p1";
                x.Consumer = "c1";
                x.Cluster = "Sandbox";
            });

            services.AddHostedService<ProduceSample>();
            services.AddHostedService<ConsumeSample>();

        }).Build().Run();
    }
}

internal class ProduceSample : BackgroundService
{
    private readonly IKafkaFactory _kafkaFactory;
    private readonly ILogger<ProduceSample> _logger;

    public ProduceSample(IKafkaFactory kafkaFactory, ILogger<ProduceSample> logger)
    {
        this._kafkaFactory = kafkaFactory ?? throw new ArgumentNullException(nameof(kafkaFactory));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // create previously configured producer config by explicit name
        var cfg = this._kafkaFactory.CreateProducerConfig("p1");

        // or create previously configured producer config using previously configured default name
        // var cfg = this._kafkaFactory.CreateProducerConfig();

        // optionally refine config in runtime 
        cfg.Debug = "all";

        // or create producer config without IKafkaFactory
        //ProducerConfig cfg = new ProducerConfig { EnableDeliveryReports = true};

        // Create producer instance using producer config and previously configured client config with explicit name
        using var producer = this._kafkaFactory.CreateProducer<string, string>(cfg, "Sandbox", builder =>
        {
            // optionally setup producer builder
        });

        while (!stoppingToken.IsCancellationRequested)
        {
            var deliveryResult = await producer.ProduceAsync("test1", 
                new Message<string, string> { Key = "Test", Value = "Test" },
                stoppingToken);

            this._logger.LogInformation("{Status} {TopicPartitionOffset}", deliveryResult.Status, deliveryResult.TopicPartitionOffset);

            await Task.Delay(3000, stoppingToken);
        }
    }
}

internal class ConsumeSample : BackgroundService
{
    private readonly IKafkaFactory _kafkaFactory;
    private readonly ILogger<ConsumeSample> _logger;

    public ConsumeSample(IKafkaFactory kafkaFactory, ILogger<ConsumeSample> logger)
    {
        this._kafkaFactory = kafkaFactory ?? throw new ArgumentNullException(nameof(kafkaFactory));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5000, stoppingToken);

        // optionally create previously configured consumer config by explicit name
        ConsumerConfig cfg = this._kafkaFactory.CreateConsumerConfig("c1");

        // or create previously configured consumer config using previously configured default name
        //ConsumerConfig cfg = this._kafkaFactory.CreateConsumerConfig();

        // optionally refine config in runtime 
        cfg.EnableAutoCommit = true;
        cfg.AutoOffsetReset = AutoOffsetReset.Earliest;

        // or create consumer config without IKafkaFactory
        // ConsumerConfig cfg = = new ConsumerConfig { GroupId = "test2" };

        // get schema registry client for previously configured client config with explicit name
        ISchemaRegistryClient sr = this._kafkaFactory.GetOrCreateSchemaRegistryClient("Sandbox");

        // Create consumer instance using consumer config and previously configured client config with explicit name
        using var consumer = this._kafkaFactory.CreateConsumer<string, string>(cfg, "Sandbox", builder =>
        {
            // optionally setup consumer builder
        });

        consumer.Subscribe("test1");

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result = consumer.Consume(stoppingToken);

            if (result != null)
            {
                this._logger.LogInformation("Consumed {TopicPartitionOffset}", result.TopicPartitionOffset);
            }
        }
    }
}