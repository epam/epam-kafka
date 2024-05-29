// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Microsoft.Extensions.DependencyInjection;

using Xunit;
using Xunit.Abstractions;

using Epam.Kafka.Tests.Common;
using Epam.Kafka.PubSub.Publication;

#if EF6
using Epam.Kafka.PubSub.EntityFramework6.Publication.Contracts;
using Epam.Kafka.PubSub.EntityFramework6.Tests.Helpers;
using Epam.Kafka.PubSub.Tests.Helpers;

namespace Epam.Kafka.PubSub.EntityFramework6.Tests;
#else
using Epam.Kafka.PubSub.EntityFrameworkCore.Publication.Contracts;
using Epam.Kafka.PubSub.EntityFrameworkCore.Tests.Helpers;
using Epam.Kafka.PubSub.Tests.Helpers;

namespace Epam.Kafka.PubSub.EntityFrameworkCore.Tests;
#endif


public class DbContextEntityPublicationHandlerTests : TestWithContext
{
    public DbContextEntityPublicationHandlerTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData(1, KafkaPublicationState.Queued, 0)]
    [InlineData(0, KafkaPublicationState.None, 0)]
    [InlineData(0, KafkaPublicationState.Committed, 0)]
    [InlineData(1, KafkaPublicationState.Error, 0)]
    [InlineData(1, KafkaPublicationState.Error, -5000)]
    [InlineData(0, KafkaPublicationState.Error, 5000)]
    [InlineData(1, KafkaPublicationState.Delivered, 0)]
    [InlineData(1, KafkaPublicationState.Delivered, -5000)]
    [InlineData(0, KafkaPublicationState.Delivered, 5000)]
    public void GetBatchSingle(int expected, KafkaPublicationState state, int nbfSecondsRelativeToNow)
    {
        this.Services.AddScoped<TestDbContextEntityPublicationHandler>();

        this.SeedData(new TestEntityDb
        {
            Id = 1,
            ExternalId = "eid1",
            KafkaPubState = state,
            KafkaPubNbf = DateTime.UtcNow.AddSeconds(nbfSecondsRelativeToNow)
        });

        using IServiceScope scope = this.ServiceProvider.CreateScope();

        TestDbContextEntityPublicationHandler pub =
            scope.ServiceProvider.GetRequiredService<TestDbContextEntityPublicationHandler>();

        IReadOnlyCollection<TopicMessage<string, TestEntityKafka>> batch = pub.GetBatch(1, false,
            CancellationToken.None);

        Assert.Equal(expected, batch.Count);
    }

    [Theory]
    [InlineData(ErrorCode.NoError, PersistenceStatus.Persisted, null, KafkaPublicationState.Committed)]
    [InlineData(ErrorCode.NoError, PersistenceStatus.Persisted, 5000, KafkaPublicationState.Delivered)]
    public void ReportResultsSingle(ErrorCode errorCode, PersistenceStatus persistenceStatus,
        int? transactionEndRelativeToNow, KafkaPublicationState expectedState)
    {
        this.Services.AddScoped<TestDbContextEntityPublicationHandler>();

        this.SeedData(new TestEntityDb
        {
            Id = 1,
            ExternalId = "eid1",
            KafkaPubState = KafkaPublicationState.Queued,
            KafkaPubNbf = DateTime.MinValue
        });

        using IServiceScope scope = this.ServiceProvider.CreateScope();

        TestDbContextEntityPublicationHandler pub =
            scope.ServiceProvider.GetRequiredService<TestDbContextEntityPublicationHandler>();

        IReadOnlyCollection<TopicMessage<string, TestEntityKafka>> batch = pub.GetBatch(1, false,
            CancellationToken.None);

        int anyOffset = 56;
        int anyPartition = 41;

        pub.ReportResults(
            batch.ToDictionary(x => x,
                _ => new DeliveryReport("any", anyPartition, anyOffset, new Error(errorCode, "reason"),
                    persistenceStatus,
                    Timestamp.Default)),
            transactionEndRelativeToNow.HasValue
                ? DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(transactionEndRelativeToNow.Value))
                : null,
            CancellationToken.None);

        TestEntityDb entity = pub.ContextInternal.Set<TestEntityDb>().Single();

        Assert.Equal(anyOffset, entity.KafkaPubOffset);
        Assert.Equal(anyPartition, entity.KafkaPubPartition);
        Assert.Equal(expectedState, entity.KafkaPubState);

        if (transactionEndRelativeToNow.HasValue)
        {
            pub.TransactionCommitted(CancellationToken.None);
            Assert.Equal(KafkaPublicationState.Committed, entity.KafkaPubState);
        }
    }

    [Theory]
    [InlineData(null, ErrorCode.NoError, PersistenceStatus.Persisted, KafkaPublicationState.Queued)]
    [InlineData(5000, ErrorCode.NoError, PersistenceStatus.Persisted, KafkaPublicationState.Queued)]
    [InlineData(null, ErrorCode.Local_ValueSerialization, PersistenceStatus.NotPersisted, KafkaPublicationState.Queued)]
    [InlineData(5000, ErrorCode.Local_ValueSerialization, PersistenceStatus.NotPersisted, KafkaPublicationState.Queued)]
    public void ReportConcurrency(int? transactionEndRelativeToNow, ErrorCode errorCode, PersistenceStatus status,
        KafkaPublicationState expectedState)
    {
        this.Services.AddScoped<TestDbContextEntityPublicationHandler>();

        // unused, but needed to stop pipeline
        TestObserver observer = new(this, nameof(TestDbContextEntityPublicationHandler), 1);

        this.SeedData(new TestEntityDb
        {
            Id = 1,
            ExternalId = "eid1",
            KafkaPubState = KafkaPublicationState.Queued,
            KafkaPubNbf = DateTime.MinValue
        });

        using IServiceScope scope = this.ServiceProvider.CreateScope();

        TestDbContextEntityPublicationHandler pub =
            scope.ServiceProvider.GetRequiredService<TestDbContextEntityPublicationHandler>();

        IReadOnlyCollection<TopicMessage<string, TestEntityKafka>> batch = pub.GetBatch(1, false,
            CancellationToken.None);

        using (IServiceScope scope1 = this.ServiceProvider.CreateScope())
        {
            TestContext context1 = scope1.ServiceProvider.GetRequiredService<TestContext>();
            TestEntityDb entity1 = context1.Set<TestEntityDb>().Single();
            entity1.Timestamp = DateTime.UtcNow.AddTicks(1);
            context1.SaveChanges();
        }

        int anyOffset = 56;
        int anyPartition = 41;

        pub.ReportResults(
            batch.ToDictionary(x => x,
                _ => new DeliveryReport("any", anyPartition, anyOffset, new Error(errorCode),
                    status,
                    Timestamp.Default)),
            transactionEndRelativeToNow.HasValue
                ? DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(transactionEndRelativeToNow.Value))
                : null,
            CancellationToken.None);

        if (transactionEndRelativeToNow.HasValue)
        {
            pub.TransactionCommitted(CancellationToken.None);
        }

        using IServiceScope scope2 = this.ServiceProvider.CreateScope();
        TestContext context2 = scope2.ServiceProvider.GetRequiredService<TestContext>();
        TestEntityDb entity2 = context2.Set<TestEntityDb>().Single();

        Assert.Equal(expectedState, entity2.KafkaPubState);
    }
}