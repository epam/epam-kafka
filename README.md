# Epam Kafka

Set of .NET libraries aimed to simplify Confluent.Kafka usage by leveraging best patterns and practices. 
Also it is a framework for building pub/sub batch processing applications.

## Features

* Abstraction to create various kafka related objects with fluent configuration in `IServiceCollection`.
* [Options pattern](https://learn.microsoft.com/en-us/dotnet/core/extensions/options) implementation for consumer, producer, client, and schema registry configs.
* Hosted service to read/write data from/to kafka with following features:
	* Processing in batches.
	* Retry mechanism for batch and for entire pipeline.
	* Parallel and sequential processing configuration.
	* Health checks.
	* System.Diagnostics.Metrics for key operations.
	* Delayed start, ability to wait for dependencies (e.g. database migrations).

### Subscription specific

 * Subscribe and internal offsets (default): kafka internal offsets storage. Partition assignment performed by group�s coordinator kafka broker.
 * Subscribe and external offsets: `IExternalOffsetsStorage` interface implementation for offsets storage. Partition assignment performed by group�s coordinator kafka broker.
 * Assign and external offsets: `IExternalOffsetsStorage` interface implementation for offsets storage. Partition assigmnent based on configuration.
 * Default implementation for `IExternalOffsetsStorage` using EntityFramework (Core and EF6).
 * Default implementation for subscription that store data using EntityFramework (Core and EF6). Possibility to commit data and offsets in same database transaction.

### Publication specific

 * Default implementation to publish data from EntityFramework (Core and EF6) context.
 * Support for transactional producers. 

## Samples

 * Dependency injection for Confluent.Kafka consumer, producer, client with named configs and options pattern. https://github.com/epam/epam-kafka/tree/develop/sample/ProduceAndConsume

## Packages

 *  [Epam.Kafka](https://www.nuget.org/packages/Epam.Kafka)
 *  [Epam.Kafka.PubSub](https://www.nuget.org/packages/Epam.Kafka.PubSub)
 *  [Epam.Kafka.PubSub.EntityFrameworkCore](https://www.nuget.org/packages/Epam.Kafka.PubSub.EntityFrameworkCore)
 *  [Epam.Kafka.PubSub.EntityFramework6](https://www.nuget.org/packages/Epam.Kafka.PubSub.EntityFramework6)

## License

[MIT](LICENSE.md)