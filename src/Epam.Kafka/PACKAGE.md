# Epam.Kafka

## About

[Epam.Kafka](https://www.nuget.org/packages/Epam.Kafka) package provides `AddKafka` extension methods for `IServiceCollection`, `IKafkaFactory` interface and its default implementation. This provides the ability to set up named `ConsumerConfig`, `IConsumer<TKey, TValue>`, `ProducerConfig`, `IProducer<TKey, TValue>`, `IClient`, `ISchemaRegistryClient` configurations in a DI container and later retrieve them via an injected `IKafkaFactory` instance.

## Key Features

* Fluently set up multiple `ConsumerConfig`, `IConsumer<TKey, TValue>`, `ProducerConfig`, `IProducer<TKey, TValue>`, `IClient`, `ISchemaRegistryClient` configurations for applications that use DI via `AddKafka` extension method.
* `KafkaFactory` caches `IClient`, `ISchemaRegistryClient` instances per configuration name, which allows to reuse resources.
* Statistics representation to deserialize json returned by statistics handler.
* Shared clients with observable errors and statistics for creation of dependent admin client and dependent producer.

## How to Use

### Configuring IKafkaFactory using fluent API

```
KafkaBuilder kafkaBuilder = services.AddKafka();

kafkaBuilder.WithClusterConfig("Sandbox").Configure(options =>
    {
        options.ClientConfig.BootstrapServers = "localhost:9092";
        options.ClientConfig.AllowAutoCreateTopics = true;

        options.SchemaRegistryConfig.Url = "localhost:8081";
    });

kafkaBuilder.WithConsumerConfig("Default").Configure(options =>
    {
        options.ConsumerConfig.GroupId = "consumer.epam-kafka-sample";
    });
```

### Using the configured IKafkaFactory

```
public class ConsumerSample : BackgroundService
{
    private readonly IKafkaFactory _kafkaFactory;

    public ConsumerSample(IKafkaFactory kafkaFactory)
    {
        this._kafkaFactory = kafkaFactory ?? throw new ArgumentNullException(nameof(kafkaFactory));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {        
        ConsumerConfig config = this._kafkaFactory.CreateConsumerConfig("Default");

        using IConsumer<string, string> consumer = this._kafkaFactory.CreateConsumer<string, string>(config, "Sandbox");

        consumer.Subscribe("epam-kafka-sample-topic-123");

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result = consumer.Consume(stoppingToken);

            Console.WriteLine($"Consumed {result.TopicPartitionOffset}");
        }
    }
}
```

### Configuring IKafkaFactory using IConfiguration

By default `IKafkaFactory` configured from `IConfiguration` registered in `IServiceCollection`. This configuration can be extended or modified using fluent API. Sample of json config:

```
{
  "Kafka": {
    "Default": {
      "Cluster": "Sandbox",
      "Consumer": "Default",
      "Producer": "Default"
    },
    "Clusters": {
      "Sandbox": {
        "bootstrap.servers": "localhost:9092",
        "allow.auto.create.topics": true,
        "schema.registry.url": "localhost:8081"
      }
    },
    "Producers": {
      "Default": {
        "client.id": "<DomainName>@<MachineName>"
      },
      "Transactional": {
        "client.id": "<DomainName>@<MachineName>"
        "transactional.id": "producer.epam-kafka-sample"
      }
    },
    "Consumers": {
      "Default": {
        "group.id": "consumer.epam-kafka-sample"
      }
    }    
  }
}
```