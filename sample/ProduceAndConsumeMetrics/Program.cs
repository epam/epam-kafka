// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;

namespace ProduceAndConsumeMetrics;

internal class Program
{
    private static void Main(string[] args)
    {
        IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

        hostBuilder.ConfigureServices(services =>
        {
            services.AddOpenTelemetry().WithMetrics(
                mb => mb.AddMeter(
                        // Choose required meters (see https://github.com/epam/epam-kafka/wiki/Low-level-consumer,-producer,-and-admin-client#metrics)
                        Statistics.TopLevelMeterName, 
                        Statistics.TopicPartitionMeterName)
                    .AddReader(new PeriodicExportingMetricReader(new CustomConsoleExporter(), 1000)));

            KafkaBuilder kafkaBuilder = services.AddKafka();

            // use mock cluster for demo purposes only, NOT for production!
            kafkaBuilder.WithTestMockCluster("Sandbox");

            kafkaBuilder.WithClusterConfig("Sandbox").Configure(x =>
            {
                // To enable metrics set 'statistics.interval.ms' and don't assign custom statistics handler in consumer builder.
                // If you need both metrics and access to statistics use consumer.Subscribe(custom observer);
                x.ClientConfig.StatisticsIntervalMs = 1000;
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
        ProducerConfig cfg = this._kafkaFactory.CreateProducerConfig();

        using IProducer<string, string> producer = this._kafkaFactory.CreateProducer<string, string>(cfg);

        while (!stoppingToken.IsCancellationRequested)
        {
            DeliveryResult<string, string> deliveryResult = await producer.ProduceAsync("test1",
                new Message<string, string> { Key = "Test", Value = "Test" },
                stoppingToken);

            this._logger.LogInformation("{Status} {TopicPartitionOffset}", deliveryResult.Status, deliveryResult.TopicPartitionOffset);

            await Task.Delay(5000, stoppingToken);
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

        ConsumerConfig cfg = this._kafkaFactory.CreateConsumerConfig();

        // Create consumer instance using consumer config and previously configured client config with explicit name
        using IConsumer<string, string> consumer = this._kafkaFactory.CreateConsumer<string, string>(cfg);

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

internal class CustomConsoleExporter : ConsoleExporter<Metric>
{

    public CustomConsoleExporter() : base(new ConsoleExporterOptions())
    {
    }

    public override ExportResult Export(in Batch<Metric> batch)
    {
        Console.Clear();
        foreach (Metric m in batch)
        {
            foreach (MetricPoint point in m.GetMetricPoints())
            {
                var pt = new List<string>();

                if (m.MeterTags is not null)
                {
                    foreach (KeyValuePair<string, object?> tag in m.MeterTags)
                    {
                        pt.Add(tag.ToString());
                    }
                }
                
                foreach (KeyValuePair<string, object?> tag in point.Tags)
                {
                    pt.Add(tag.ToString());
                }

                string tags = string.Join(",", pt);

                string value = "???";

                if (m.MetricType == MetricType.LongGauge)
                {
                    value = point.GetGaugeLastValueLong().ToString("D");
                }
                else if (m.MetricType == MetricType.LongSum)
                {
                    value = point.GetSumLong().ToString("D");
                }

                Console.WriteLine($"{m.Name}[{tags}]= {value}");
            }
        }

        return ExportResult.Success;
    }
}