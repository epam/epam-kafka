// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.PubSub.Tests.Helpers;
using Epam.Kafka.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.IntegrationTests.Subscription.CombinedState;

[Collection(SubscribeTests.Name)]
public class ReadTests : TestWithServices, IClassFixture<MockCluster>
{
    private readonly MockCluster _mockCluster;

    public ReadTests(ITestOutputHelper output, MockCluster mockCluster) : base(output)
    {
        this._mockCluster = mockCluster ?? throw new ArgumentNullException(nameof(mockCluster));
    }

    [Theory]
    [InlineData(PartitionAssignmentStrategy.CooperativeSticky)]
    [InlineData(PartitionAssignmentStrategy.Range)]
    [InlineData(PartitionAssignmentStrategy.RoundRobin)]
    public async Task OneBatchTwoPartitions(PartitionAssignmentStrategy assignmentStrategy)
    {
        TopicPartition tp1 = new(this.AnyTopicName, 1);
        TopicPartition tp2 = new(this.AnyTopicName, 2);

        using TestObserver observer = new(this, 3);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer, 0, 3);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster,assignmentStrategy: assignmentStrategy)
            .WithValueDeserializer(_ => deserializer)
            .WithSubscribeAndExternalOffsets<TestOffsetsStorage>();

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 5, tp1);
        Dictionary<TestEntityKafka, TopicPartitionOffset> m2 = await MockCluster.SeedKafka(this, 5, tp2);

        handler.WithSuccess(2, m1.Concat(m2));
        deserializer.WithSuccess(2, m1.Keys.Concat(m2.Keys).ToArray());

        offsets.WithGet(2, new TopicPartitionOffset(tp1, Offset.Unset), new TopicPartitionOffset(tp2, Offset.Unset));
        offsets.WithSet(2, new TopicPartitionOffset(tp1, 0), new TopicPartitionOffset(tp2, 0));
        offsets.WithSetAndGetForNextIteration(2, new TopicPartitionOffset(tp1, 5), new TopicPartitionOffset(tp2, 5));

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();
        offsets.Verify();

        // iteration 1
        observer.AssertSubNotAssigned();

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(10, true);
        observer.AssertProcess();
        observer.AssertCommitExternal();
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
        var offsets = new TestOffsetsStorage(observer, 0, 1, 2);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer)
            .WithSubscribeAndExternalOffsets<TestOffsetsStorage>().WithOptions(x => x.BatchSize = 5);

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 5, tp3);
        Dictionary<TestEntityKafka, TopicPartitionOffset> m2 = await MockCluster.SeedKafka(this, 5, tp3);

        handler.WithSuccess(2, m1);
        handler.WithSuccess(3, m2);
        deserializer.WithSuccess(2, m1.Keys.ToArray());
        deserializer.WithSuccess(3, m2.Keys.ToArray());

        var unset = new TopicPartitionOffset(tp3, Offset.Unset);
        var autoReset = new TopicPartitionOffset(tp3, 0);
        var offset5 = new TopicPartitionOffset(tp3, 5);
        var offset10 = new TopicPartitionOffset(tp3, 10);

        offsets.WithGet(2, unset);

        offsets.WithSet(2, autoReset);
        offsets.WithSetAndGetForNextIteration(2, offset5);
        offsets.WithSetAndGetForNextIteration(3, offset10);

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();
        offsets.Verify();

        // iteration 1
        observer.AssertSubNotAssigned();

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(5, true);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 3
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(5);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 4
        observer.AssertSubEmpty();
    }

    [Fact]
    public async Task AutoOffsetResetLatest()
    {
        TopicPartition tp3 = new(this.AnyTopicName, 3);

        using TestObserver observer = new(this, 3);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer, 0, 1, 2);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster, AutoOffsetReset.Latest).WithValueDeserializer(_ => deserializer)
            .WithSubscribeAndExternalOffsets<TestOffsetsStorage>().WithOptions(x =>
            {
                x.BatchSize = 5;
            });

        await MockCluster.SeedKafka(this, 5, tp3);

        offsets.WithGet(2, new TopicPartitionOffset(tp3, Offset.Unset));
        offsets.WithSetAndGetForNextIteration(2, new TopicPartitionOffset(tp3, 5));

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();
        offsets.Verify();

        // iteration 1
        observer.AssertSubNotAssigned();

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(0, true);
        observer.AssertStop(SubscriptionBatchResult.Empty); ;

        // iteration 3
        observer.AssertSubEmpty();
    }

    [Fact]
    public async Task AutoOffsetResetError()
    {
        TopicPartition tp3 = new(this.AnyTopicName, 3);

        using TestObserver observer = new(this, 2);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer, 0, 1, 2);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster, AutoOffsetReset.Error).WithValueDeserializer(_ => deserializer)
            .WithSubscribeAndExternalOffsets<TestOffsetsStorage>().WithOptions(x =>
            {
                x.BatchSize = 5;
            });

        await MockCluster.SeedKafka(this, 5, tp3);

        //offsets.WithGet(1, new TopicPartitionOffset(tp3, Offset.Unset));
        offsets.WithGet(2, new TopicPartitionOffset(tp3, Offset.Unset));

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();
        offsets.Verify();

        observer.AssertSubNotAssigned();

        // iteration 1
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead();
        observer.AssertStop<KafkaException>("Local: No offset stored");
    }
}