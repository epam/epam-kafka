# Epam.Kafka.PubSub.EntityFramework6

## About

[Epam.Kafka.PubSub.EntityFramework6](https://www.nuget.org/packages/Epam.Kafka.PubSub.EntityFramework6) package provides entity framework 6 implementation for key abstractions defined in [Epam.Kafka.PubSub](https://www.nuget.org/packages/Epam.Kafka.PubSub).

## Key Features

* Base abstract classes `DbContextSubscriptionHandler<TKey, TValue, TContext>`, `DbContextEntitySubscriptionHandler<TKey, TValue, TContext TEntity>` to help with implementation of `ISubscriptionHandler<TKey, TValue>` that read data from KAFKA topics and save it to database using `DbContext`.
* Default implementation of `IExternalOffsetsStorage` that store offsets in database via `DbContext`. `TryAddKafkaDbContextState` extension method to register it in `IServiceCollection`. 
* Base abstract classes `DbContextPublicationHandler<TKey, TValue, TEntity, TContext>` and `DbContextEntityPublicationHandler<TKey, TValue, TEntity, TContext>` to help with implementation of `IPublicationHandler<TKey, TValue>` that read data from database using `DbContext` and publish it to kafka topics.

## How to Use

### Store subscription offsets

Prepare context.
```
public class SampleDbContext : DbContext, IKafkaStateDbContext
{
    public DbSet<KafkaTopicState> KafkaTopicStates => this.Set<KafkaTopicState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddKafkaState();
    }
}
```

Register services and setup topic partition assignment and offsets storage for subscription.

```
services.TryAddKafkaDbContextState<SampleDbContext>();

KafkaBuilder kafkaBuilder = services.AddKafka();
kafkaBuilder.AddSubscription<string, KafkaEntity, SubscriptionHandlerSample>("Sample")
            .WithSubscribeAndExternalOffsets();
```

### Publish data from database to kafka

Optionally derive entity from `IKafkaPublicationEntity` interface to use default state management.

```
public class SamplePublicationEntity : IKafkaPublicationEntity
{
    public KafkaPublicationState KafkaPubState { get; set; }

    public DateTime KafkaPubNbf { get; set; }
}
```

Create publication handler derived from `DbContextEntityPublicationHandler<TKey, TValue, TEntity, TContext>` for entity with default state management or from `DbContextPublicationHandler<TKey, TValue, TEntity, TContext>` for custom state management.

Setup publication
```
KafkaBuilder kafkaBuilder = services.AddKafka();
kafkaBuilder.AddPublication<string, KafkaEntity, SamplePublicationHandler>("Sample");
```