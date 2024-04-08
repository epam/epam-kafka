// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Subscription.Options;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.PubSub.Tests.Helpers;
using Epam.Kafka.Tests.Common;

using Microsoft.Extensions.DependencyInjection;

using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.IntegrationTests.Subscription.ExternalState;

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

        using TestObserver observer = new(this, 2);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer)
            .WithAssignAndExternalOffsets<TestOffsetsStorage>().WithOptions(x => x.WithTopicPartitions(tp1, tp2));

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 5, tp1);
        Dictionary<TestEntityKafka, TopicPartitionOffset> m2 = await MockCluster.SeedKafka(this, 5, tp2);

        handler.WithSuccess(1, m1.Concat(m2));
        deserializer.WithSuccess(1, m1.Keys.ToArray());
        deserializer.WithSuccess(1, m2.Keys.ToArray());

        var p1Unset = new TopicPartitionOffset(tp1, Offset.Unset);
        var p2Unset = new TopicPartitionOffset(tp2, Offset.Unset);
        var p1AutoReset = new TopicPartitionOffset(tp1, 0);
        var p2AutoReset = new TopicPartitionOffset(tp2, 0);
        var p1Offset5 = new TopicPartitionOffset(tp1, 5);
        var p2Offset5 = new TopicPartitionOffset(tp2, 5);

        offsets.WithGet(1, p1Unset, p2Unset);
        offsets.WithSet(1, p1AutoReset, p2AutoReset);
        offsets.WithSetAndGetForNextIteration(1, p1Offset5, p2Offset5);

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();
        offsets.Verify();

        // iteration 1
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(10);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 2
        observer.AssertSubEmpty();
    }

    [Fact]
    public async Task OnePartitionTwoBatches()
    {
        TopicPartition tp1 = new(this.AnyTopicName, 1);

        using TestObserver observer = new(this, 3);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer)
            .WithAssignAndExternalOffsets<TestOffsetsStorage>().WithOptions(x =>
            {
                x.WithTopicPartitions(new TopicPartition(this.AnyTopicName, 1));
                x.BatchSize = 5;
            });

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 5, tp1);
        Dictionary<TestEntityKafka, TopicPartitionOffset> m2 = await MockCluster.SeedKafka(this, 5, tp1);

        handler.WithSuccess(1, m1);
        handler.WithSuccess(2, m2);

        deserializer.WithSuccess(1, m1.Keys.ToArray());
        deserializer.WithSuccess(2, m2.Keys.ToArray());

        offsets.WithGet(1, new TopicPartitionOffset(tp1, Offset.Unset));
        offsets.WithSet(1, new TopicPartitionOffset(tp1, 0)); // auto reset
        offsets.WithSetAndGetForNextIteration(1, new TopicPartitionOffset(tp1, 5));
        offsets.WithSetAndGetForNextIteration(2, new TopicPartitionOffset(tp1, 10));

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();
        offsets.Verify();

        // iteration 1
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(5);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(5);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 3
        observer.AssertSubEmpty();
    }

    [Fact]
    public async Task AutoOffsetResetLatest()
    {
        TopicPartition tp1 = new(this.AnyTopicName, 1);

        using TestObserver observer = new(this, 2);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster, AutoOffsetReset.Latest).WithValueDeserializer(_ => deserializer)
            .WithAssignAndExternalOffsets<TestOffsetsStorage>().WithOptions(x =>
            {
                x.WithTopicPartitions(new TopicPartition(this.AnyTopicName, 1));
                x.BatchSize = 5;
            });

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 5, tp1);

        offsets.WithGet(1, new TopicPartitionOffset(tp1, Offset.Unset));
        offsets.WithSetAndGetForNextIteration(1, new TopicPartitionOffset(tp1, 5));

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();
        offsets.Verify();

        // iteration 1
        observer.AssertSubNotAssigned();

        // iteration 2
        observer.AssertSubEmpty();
    }

    [Fact]
    public async Task AutoOffsetResetError()
    {
        TopicPartition tp1 = new(this.AnyTopicName, 1);

        using TestObserver observer = new(this, 1);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster, AutoOffsetReset.Error).WithValueDeserializer(_ => deserializer)
            .WithAssignAndExternalOffsets<TestOffsetsStorage>().WithOptions(x =>
            {
                x.WithTopicPartitions(new TopicPartition(this.AnyTopicName, 1));
                x.BatchSize = 5;
            });

        await MockCluster.SeedKafka(this, 5, tp1);

        offsets.WithGet(1, new TopicPartitionOffset(tp1, Offset.Unset));
        //offsets.WithGet(2, new TopicPartitionOffset(tp1, Offset.Unset));

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();
        offsets.Verify();

        // iteration 1
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertStop<KafkaException>("Local: No offset stored");
    }
}