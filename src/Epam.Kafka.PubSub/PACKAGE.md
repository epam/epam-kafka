# Epam.Kafka.PubSub

## About

[Epam.Kafka.PubSub](https://www.nuget.org/packages/Epam.Kafka.PubSub) package provides `AddSubscription` and `AddPublication` extension methods for `KafkaBuilder`. This provides the ability to set up default implementations of `IHostedService` to read/write messages to/from kafka and proccess them in batches with:
	* Retry mechanism for batch and for entire pipeline.
	* Parallel and sequential processing configuration.
	* Health checks.
	* System.Diagnostics.Metrics for key operations.
	* Delayed start, ability to wait for dependencies (e.g. database migrations).

## Key Features

* Define `ISubscriptionHandler<TKey, TValue>` interface and provide base abstract class `SubscriptionHandler<TKey, TValue>` to simplify creation of batch message processing logic for kafka subscriptions.
* Define `IExternalOffsetsStorage` interface to store processed message offsets externally. Default implementation that use EntityFrameworkCore available in related package [Epam.Kafka.PubSub.EntityFrameworkCore](https://www.nuget.org/packages/Epam.Kafka.PubSub.EntityFrameworkCore)
* Define `IPublicationHandler<TKey, TValue>` interface and provide base abstract class `PublicationHandler<TKey, TValue, TEntity>` to simplify creation of batch processing logic for data publishers.

## How to Use

### Create subscription

Create class derived from `SubscriptionHandler<TKey, TValue>` or `ISubscriptionHandler<TKey, TValue>`.

Register services and configure subscription.

```
KafkaBuilder kafkaBuilder = services.AddKafka();
kafkaBuilder.AddSubscription<string, KafkaEntity, SubscriptionHandlerSample>("Sample");	
```

### Create publication

Create class derived from `IPublicationHandler<TKey, TValue>`.

Register services and configure publication.

```
KafkaBuilder kafkaBuilder = services.AddKafka();
kafkaBuilder.AddPublication<string, KafkaEntity, PublicationHandlerSample>("Sample")
```