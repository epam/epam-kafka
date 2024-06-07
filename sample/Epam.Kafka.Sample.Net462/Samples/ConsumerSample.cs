// Copyright © 2024 EPAM Systems

using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;

namespace Epam.Kafka.Sample.Net462.Samples
{
    public class ConsumerSample : BackgroundService
    {
        private readonly IKafkaFactory _kafkaFactory;

        public ConsumerSample(IKafkaFactory kafkaFactory)
        {
            this._kafkaFactory = kafkaFactory ?? throw new ArgumentNullException(nameof(kafkaFactory));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(3000, stoppingToken);

            ConsumerConfig config = this._kafkaFactory.CreateConsumerConfig();
            config.GroupId = "anyGroup"; // optionally modify config values

            using (IConsumer<string, string> consumer = this._kafkaFactory.CreateConsumer<string, string>(config))
            {
                consumer.Subscribe("epam-kafka-sample-topic-1");

                while (!stoppingToken.IsCancellationRequested)
                {
                    ConsumeResult<string, string> result = consumer.Consume(stoppingToken);

                    Console.WriteLine($"Consumed {result.TopicPartitionOffset}");
                }
            }
        }
    }
}