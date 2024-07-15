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

namespace Epam.Kafka.PubSub.IntegrationTests.Subscription.ExternalState;

[Collection(SubscribeTests.Name)]
public class ResetTests : TestWithServices, IClassFixture<MockCluster>
{
    private readonly MockCluster _mockCluster;

    public ResetTests(ITestOutputHelper output, MockCluster mockCluster) : base(output)
    {
        this._mockCluster = mockCluster ?? throw new ArgumentNullException(nameof(mockCluster));
    }

    [Theory]
    [InlineData(false, 10)]
    [InlineData(false, -1)]
    [InlineData(true, 10)]
    [InlineData(true, -1)]
    public async Task ResetToEnd(bool onCommit, long end)
    {
        TopicPartition tp3 = new(this.AnyTopicName, 3);

        using TestObserver observer = new(this, 2);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer)
            .WithAssignAndExternalOffsets<TestOffsetsStorage>().WithOptions(x =>
            {
                x.WithTopicPartitions(tp3);
                x.BatchSize = 5;
            });

        var m1 = (await MockCluster.SeedKafka(this, 10, tp3)).Take(5).ToDictionary(p => p.Key, p => p.Value);

        handler.WithSuccess(1, m1);
        deserializer.WithSuccess(1, m1.Keys.ToArray());

        var offset0 = new TopicPartitionOffset(tp3, 0);
        var offset5 = new TopicPartitionOffset(tp3, 5);
        var offsetEnd = new TopicPartitionOffset(tp3, end);

        offsets.WithGet(1, offset0);

        if (onCommit)
        {
            offsets.WithReset(1, offset5, offsetEnd);
        }
        else
        {
            offsets.WithSet(1, offset5);
        }

        offsets.WithGet(2, offsetEnd);

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();
        offsets.Verify();

        // iteration 1
        observer.AssertStart();
        observer.AssertAssign(true);
        observer.AssertRead(5);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 2
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

        using TestObserver observer = new(this, 2);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer)
            .WithAssignAndExternalOffsets<TestOffsetsStorage>().WithOptions(x =>
            {
                x.WithTopicPartitions(tp3);
                x.BatchSize = 5;
            });

        var m1 = (await MockCluster.SeedKafka(this, 10, tp3)).Take(5).ToDictionary(p => p.Key, p => p.Value);

        handler.WithSuccess(1, m1);
        deserializer.WithSuccess(1, m1.Keys.ToArray());

        var offset0 = new TopicPartitionOffset(tp3, 0);
        var offset5 = new TopicPartitionOffset(tp3, 5);
        var beginning = new TopicPartitionOffset(tp3, start);

        offsets.WithGet(1, offset0);

        if (onCommit)
        {
            offsets.WithReset(1, offset5, beginning);
        }
        else
        {
            offsets.WithSet(1, offset5);
        }

        offsets.WithGet(2, beginning);
        offsets.WithSet(2, offset5);

        handler.WithSuccess(2, m1);
        deserializer.WithSuccess(2, m1.Keys.ToArray());

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();
        offsets.Verify();

        // iteration 1
        observer.AssertStart();
        observer.AssertAssign(true);
        observer.AssertRead(5);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 2
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

        using TestObserver observer = new(this, 2);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer)
            .WithAssignAndExternalOffsets<TestOffsetsStorage>().WithOptions(x =>
            {
                x.WithTopicPartitions(tp3);
                x.BatchSize = 5;
            });

        var seedKafka = await MockCluster.SeedKafka(this, 10, tp3);

        var m1 = seedKafka.Take(5).ToDictionary(p => p.Key, p => p.Value);
        var m2 = seedKafka.Skip(8).ToDictionary(p => p.Key, p => p.Value);

        handler.WithSuccess(1, m1);
        deserializer.WithSuccess(1, m1.Keys.ToArray());

        handler.WithSuccess(2, m2);
        deserializer.WithSuccess(2, m2.Keys.ToArray());

        var offset0 = new TopicPartitionOffset(tp3, 0);
        var offset5 = new TopicPartitionOffset(tp3, 5);
        var next = new TopicPartitionOffset(tp3, 8);
        var offset10 = new TopicPartitionOffset(tp3, 10);

        offsets.WithGet(1, offset0);

        if (onCommit)
        {
            offsets.WithReset(1, offset5, next);
        }
        else
        {
            offsets.WithSet(1, offset5);
        }

        offsets.WithGet(2, next);
        offsets.WithSet(2, offset10);

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();
        offsets.Verify();

        // iteration 1
        observer.AssertStart();
        observer.AssertAssign(true);
        observer.AssertRead(5);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign(!onCommit);
        observer.AssertRead(2);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);
    }
}