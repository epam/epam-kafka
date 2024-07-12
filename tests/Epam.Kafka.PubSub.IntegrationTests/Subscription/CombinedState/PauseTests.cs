// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Subscription;
using Epam.Kafka.PubSub.Subscription.Options;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.PubSub.Tests.Helpers;
using Epam.Kafka.Tests.Common;

using Microsoft.Extensions.DependencyInjection;

using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.IntegrationTests.Subscription.CombinedState;

[Collection(SubscribeTests.Name)]
public class PauseTests : TestWithServices, IClassFixture<MockCluster>
{
    private readonly MockCluster _mockCluster;

    public PauseTests(ITestOutputHelper output, MockCluster mockCluster) : base(output)
    {
        this._mockCluster = mockCluster ?? throw new ArgumentNullException(nameof(mockCluster));
    }

    [Fact]
    public async Task OneRunningStartOne()
    {
        TopicPartition tp1 = new(this.AnyTopicName, 1);
        TopicPartition tp2 = new(this.AnyTopicName, 2);

        using TestObserver observer = new(this, 4);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer, 0, 3);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer)
            .WithSubscribeAndExternalOffsets<TestOffsetsStorage>();

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 5, tp1);
        Dictionary<TestEntityKafka, TopicPartitionOffset> m2 = await MockCluster.SeedKafka(this, 5, tp2);

        handler.WithSuccess(2, m2);
        handler.WithSuccess(3, m1);
        deserializer.WithSuccess(2, m2.Keys.ToArray());
        deserializer.WithSuccess(3, m1.Keys.ToArray());

        var p1OffsetEnd = new TopicPartitionOffset(tp1, ExternalOffset.Paused);

        var p1Offset0 = new TopicPartitionOffset(tp1, 0);
        var p2Offset0 = new TopicPartitionOffset(tp2, 0);

        var p1Offset5 = new TopicPartitionOffset(tp1, 5);
        var p2Offset5 = new TopicPartitionOffset(tp2, 5);

        offsets.WithGet(2, p1OffsetEnd, p2Offset0);
        offsets.WithSet(2, p2Offset5);

        offsets.WithGet(3, p1Offset0, p2Offset5);
        offsets.WithSet(3, p1Offset5);

        offsets.WithGet(4, p1Offset5, p2Offset5);

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();
        offsets.Verify();

        // iteration 1
        observer.AssertSubNotAssigned();

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(5);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 3
        observer.AssertStart();
        observer.AssertAssign(true);
        observer.AssertRead(5);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 4
        observer.AssertSubEmpty();
    }

    [Fact]
    public async Task AllPausedStartOne()
    {
        TopicPartition tp3 = new(this.AnyTopicName, 3);

        using TestObserver observer = new(this, 3);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer, 0, 1, 2);

        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer)
            .WithSubscribeAndExternalOffsets<TestOffsetsStorage>();

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 5, tp3);

        handler.WithSuccess(3, m1);
        deserializer.WithSuccess(3, m1.Keys.ToArray());

        var end = new TopicPartitionOffset(tp3, ExternalOffset.Paused);
        var offset0 = new TopicPartitionOffset(tp3, 0);
        var offset5 = new TopicPartitionOffset(tp3, 5);

        offsets.WithGet(2, end);

        offsets.WithGet(3, offset0);
        offsets.WithSet(3, offset5);

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();
        offsets.Verify();

        // iteration 1
        observer.AssertSubNotAssigned();

        // iteration 2
        observer.AssertSubPaused();

        // iteration 3
        observer.AssertStart();
        observer.AssertAssign(true);
        observer.AssertRead(5);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PauseAndResume(bool onCommit)
    {
        TopicPartition tp3 = new(this.AnyTopicName, 3);

        using TestObserver observer = new(this, 4);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer, 0, 1, 2);

        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer)
            .WithSubscribeAndExternalOffsets<TestOffsetsStorage>().WithOptions(x =>
            {
                x.BatchSize = 5;
            });

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 5, tp3);
        Dictionary<TestEntityKafka, TopicPartitionOffset> m2 = await MockCluster.SeedKafka(this, 5, tp3);

        handler.WithSuccess(2, m1);
        deserializer.WithSuccess(2, m1.Keys.ToArray());
        handler.WithSuccess(4, m2);
        deserializer.WithSuccess(4, m2.Keys.ToArray());

        var paused = new TopicPartitionOffset(tp3, ExternalOffset.Paused);
        var offset0 = new TopicPartitionOffset(tp3, 0);
        var offset5 = new TopicPartitionOffset(tp3, 5);
        var offset10 = new TopicPartitionOffset(tp3, 10);

        offsets.WithGet(2, offset0);

        if (onCommit)
        {
            offsets.WithReset(2, offset5, paused);
        }
        else
        {
            offsets.WithSet(2, offset5);
        }

        offsets.WithGet(3, paused);
        offsets.WithGet(4, offset5);
        offsets.WithSet(4, offset10);

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();
        offsets.Verify();

        // iteration 1
        observer.AssertSubNotAssigned();

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(5);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        if (!onCommit)
        {
            observer.AssertCommitKafka();
        }
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 3
        observer.AssertSubPaused();

        // iteration 4
        observer.AssertStart();
        observer.AssertAssign(true);
        observer.AssertRead(5);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);
    }
}