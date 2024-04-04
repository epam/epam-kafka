// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Microsoft.Extensions.Hosting;

namespace Epam.Kafka.Sample.Samples;

public class ProducerSample : BackgroundService
{
    private readonly IKafkaFactory _kafkaFactory;

    public ProducerSample(IKafkaFactory kafkaFactory)
    {
        this._kafkaFactory = kafkaFactory ?? throw new ArgumentNullException(nameof(kafkaFactory));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        ProducerConfig config = this._kafkaFactory.CreateProducerConfig("Default");

        using IProducer<string, string> producer = this._kafkaFactory.CreateProducer<string, string>(config, "Sandbox");

        while (!stoppingToken.IsCancellationRequested)
        {
            DeliveryResult<string, string>? result = await producer.ProduceAsync("epam-kafka-sample-topic-1",
                new Message<string, string> { Key = "qwe", Value = "qwe" }, stoppingToken);

            Console.WriteLine($"{result.Status:G} {result.TopicPartitionOffset}");

            await Task.Delay(5000, stoppingToken);
        }
    }
}