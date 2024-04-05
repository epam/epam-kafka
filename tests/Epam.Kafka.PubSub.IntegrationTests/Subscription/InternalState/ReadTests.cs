// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.PubSub.Tests.Helpers;
using Epam.Kafka.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.IntegrationTests.Subscription.InternalState;

public class ReadTests : TestWithServices, IClassFixture<MockCluster>
{
    private readonly MockCluster _mockCluster;

    public ReadTests(ITestOutputHelper output, MockCluster mockCluster) : base(output)
    {
        this._mockCluster = mockCluster ?? throw new ArgumentNullException(nameof(mockCluster));
    }

    [Fact]
    public async Task OneBatchTwoPartitions()
    {
        TopicPartition tp1 = new(this.AnyTopicName, 1);
        TopicPartition tp2 = new(this.AnyTopicName, 2);

        using TestObserver observer = new(this, 3);

        var handler = new TestSubscriptionHandler(observer);

        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer);

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 5, tp1);
        Dictionary<TestEntityKafka, TopicPartitionOffset> m2 = await MockCluster.SeedKafka(this, 5, tp2);

        handler.WithSuccess(2, m1.Concat(m2));
        deserializer.WithSuccess(2, m1.Keys.ToArray());
        deserializer.WithSuccess(2, m2.Keys.ToArray());

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();

        // iteration 1
        observer.AssertSubNotAssigned();

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(10);
        observer.AssertProcess();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 3
        observer.AssertSubEmpty();
    }

    [Fact]
    public async Task OnePartitionTwoBatches()
    {
        TopicPartition tp3 = new(this.AnyTopicName, 3);

        using TestObserver observer = new(this, 4);

        var handler = new TestSubscriptionHandler(observer);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer).WithOptions(x => x.BatchSize = 5);

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 5, tp3);
        Dictionary<TestEntityKafka, TopicPartitionOffset> m2 = await MockCluster.SeedKafka(this, 5, tp3);

        handler.WithSuccess(2, m1);
        handler.WithSuccess(3, m2);
        deserializer.WithSuccess(2, m1.Keys.ToArray());
        deserializer.WithSuccess(3, m2.Keys.ToArray());
        
        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();

        // iteration 1
        observer.AssertSubNotAssigned();

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(5);
        observer.AssertProcess();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 3
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(5);
        observer.AssertProcess();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 4
        observer.AssertSubEmpty();
    }
}