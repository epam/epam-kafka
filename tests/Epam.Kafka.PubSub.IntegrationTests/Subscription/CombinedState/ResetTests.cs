﻿// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.PubSub.Tests.Helpers;
using Epam.Kafka.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.IntegrationTests.Subscription.CombinedState;

[Collection(SubscribeTests.Name)]
public class ResetTests : TestWithServices, IClassFixture<MockCluster>
{
    private readonly MockCluster _mockCluster;

    public ResetTests(ITestOutputHelper output, MockCluster mockCluster) : base(output)
    {
        this._mockCluster = mockCluster ?? throw new ArgumentNullException(nameof(mockCluster));
    }

    [Fact]
    public async Task OneRunningOneStarting()
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

        var p1OffsetEnd = new TopicPartitionOffset(tp1, Offset.End);

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

        var end = new TopicPartitionOffset(tp3, Offset.End);
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
    [InlineData(false, 10)]
    [InlineData(false, -1)]
    [InlineData(true, 10)]
    [InlineData(true, -1)]
    public async Task ResetToEnd(bool onCommit, long end)
    {
        TopicPartition tp3 = new(this.AnyTopicName, 3);

        using TestObserver observer = new(this, 3);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer, 0, 1, 2);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer)
            .WithSubscribeAndExternalOffsets<TestOffsetsStorage>().WithOptions(x => x.BatchSize = 5);

        var m1 = (await MockCluster.SeedKafka(this, 10, tp3)).Take(5).ToDictionary(p => p.Key, p => p.Value);

        handler.WithSuccess(2, m1);
        deserializer.WithSuccess(2, m1.Keys.ToArray());

        var offset0 = new TopicPartitionOffset(tp3, 0);
        var offset5 = new TopicPartitionOffset(tp3, 5);
        var offset10 = new TopicPartitionOffset(tp3, end);

        offsets.WithGet(2, offset0);

        if (onCommit)
        {
            offsets.WithReset(2, offset5, offset10);
        }
        else
        {
            offsets.WithSet(2, offset5);
        }

        offsets.WithGet(3, offset10);

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
        observer.AssertSubEmpty(!onCommit);
    }

    [Theory]
    [InlineData(false, -2)]
    [InlineData(false, 0)]
    [InlineData(true, -2)]
    [InlineData(true, 0)]
    public async Task ResetToBeginning(bool onCommit, long start)
    {
        TopicPartition tp3 = new(this.AnyTopicName, 3);

        using TestObserver observer = new(this, 3);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer, 0, 1, 2);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer)
            .WithSubscribeAndExternalOffsets<TestOffsetsStorage>().WithOptions(x => x.BatchSize = 5);

        var m1 = (await MockCluster.SeedKafka(this, 10, tp3)).Take(5).ToDictionary(p => p.Key, p => p.Value);

        handler.WithSuccess(2, m1);
        deserializer.WithSuccess(2, m1.Keys.ToArray());

        var offset0 = new TopicPartitionOffset(tp3, 0);
        var offset5 = new TopicPartitionOffset(tp3, 5);
        var beginning = new TopicPartitionOffset(tp3, start);

        offsets.WithGet(2, offset0);

        if (onCommit)
        {
            offsets.WithReset(2, offset5, beginning);
        }
        else
        {
            offsets.WithSet(2, offset5);
        }

        offsets.WithGet(3, beginning);
        offsets.WithSet(3, offset5);

        handler.WithSuccess(3, m1);
        deserializer.WithSuccess(3, m1.Keys.ToArray());

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
        observer.AssertAssign(!onCommit);
        observer.AssertRead(5);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadThenReset(bool onCommit)
    {
        TopicPartition tp3 = new(this.AnyTopicName, 3);

        using TestObserver observer = new(this, 3);

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

        var seedKafka = await MockCluster.SeedKafka(this, 10, tp3);

        var m1 = seedKafka.Take(5).ToDictionary(p => p.Key, p => p.Value);
        var m2 = seedKafka.Skip(8).ToDictionary(p => p.Key, p => p.Value);

        handler.WithSuccess(2, m1);
        deserializer.WithSuccess(2, m1.Keys.ToArray());

        handler.WithSuccess(3, m2);
        deserializer.WithSuccess(3, m2.Keys.ToArray());

        var offset0 = new TopicPartitionOffset(tp3, 0);
        var offset5 = new TopicPartitionOffset(tp3, 5);
        var next = new TopicPartitionOffset(tp3, 8);
        var offset10 = new TopicPartitionOffset(tp3, 10);

        offsets.WithGet(2, offset0);

        if (onCommit)
        {
            offsets.WithReset(2, offset5, next);
        }
        else
        {
            offsets.WithSet(2, offset5);
        }

        offsets.WithGet(3, next);
        offsets.WithSet(3, offset10);

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
        observer.AssertAssign(!onCommit);
        observer.AssertRead(2);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);
    }
}