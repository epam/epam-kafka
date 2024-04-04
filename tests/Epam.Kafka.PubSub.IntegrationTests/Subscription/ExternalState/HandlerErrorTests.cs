// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka.PubSub.Subscription.Options;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.PubSub.Tests.Helpers;
using Epam.Kafka.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.IntegrationTests.Subscription.ExternalState;

public class HandlerErrorTests : TestWithServices, IClassFixture<MockCluster>
{
    private readonly MockCluster _mockCluster;

    public HandlerErrorTests(ITestOutputHelper output, MockCluster mockCluster) : base(output)
    {
        this._mockCluster = mockCluster ?? throw new ArgumentNullException(nameof(mockCluster));
    }
    
    [Fact]
    public async Task TransientErrorWithAdaptiveBatchSize()
    {
        Exception exception = new TestException();

        TopicPartition tp3 = new(this.AnyTopicName, 3);

        using TestObserver observer = new(this, 7);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer)
            .WithAssignAndExternalOffsets<TestOffsetsStorage>().WithOptions(x =>
            {
                x.BatchSize = 6;
                x.WithTopicPartitions(tp3);
            });

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 6, tp3);
        Dictionary<TestEntityKafka, TopicPartitionOffset> m2 = await MockCluster.SeedKafka(this, 4, tp3);

        handler.WithError(1, exception, m1);
        handler.WithError(2, exception, m1);
        handler.WithError(3, exception, m1.Take(3));
        handler.WithError(4, exception, m1.Take(1));
        handler.WithSuccess(5, m1.Take(1));
        handler.WithSuccess(6, m1.Skip(1));
        handler.WithSuccess(7, m2);

        deserializer.WithSuccess(1, m1.Keys.ToArray());
        deserializer.WithSuccess(7, m2.Keys.ToArray());

        var unset = new TopicPartitionOffset(tp3, Offset.Unset);
        var offset1 = new TopicPartitionOffset(tp3, 1);
        var offset6 = new TopicPartitionOffset(tp3, 6);
        var offset10 = new TopicPartitionOffset(tp3, 10);

        offsets.WithGet(1, unset);
        offsets.WithGet(2, unset);
        offsets.WithGet(3, unset);
        offsets.WithGet(4, unset);
        offsets.WithGet(5, unset);
        offsets.WithSetAndGetForNextIteration(5, offset1);
        offsets.WithSetAndGetForNextIteration(6, offset6);
        offsets.WithSet(7, offset10);

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();
        offsets.Verify();

        // iteration 1
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(6);
        observer.AssertProcess();
        observer.AssertStop(exception);

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertProcess();
        observer.AssertStop(exception);

        // iteration 3
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertProcess();
        observer.AssertStop(exception);

        // iteration 4
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertProcess();
        observer.AssertStop(exception);

        // iteration 5
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 6
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 7
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(4);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);
    }

    [Fact]
    public async Task UnableToResolveHandler()
    {
        TopicPartition tp1 = new(this.AnyTopicName, 1);

        using TestObserver observer = new(this, 3);

        var offsets = new TestOffsetsStorage(observer);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster)
            .WithValueDeserializer(_ => deserializer)
            .WithAssignAndExternalOffsets<TestOffsetsStorage>()
            .WithOptions(options => { options.WithTopicPartitions(tp1); });

        Dictionary<TestEntityKafka, TopicPartitionOffset> entities = await MockCluster.SeedKafka(this, 5, tp1);
        deserializer.WithSuccess(1, entities.Keys.ToArray());
        offsets.WithGet(1, new TopicPartitionOffset(tp1, 0));

        InvalidOperationException exc = await Assert.ThrowsAsync<InvalidOperationException>(this.RunBackgroundServices);

        exc.Message.ShouldContain(
            "Unable to resolve service for type 'Epam.Kafka.PubSub.Tests.Helpers.TestObserver' while attempting");

        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead();
        observer.AssertStop(exc);
    }

    [Fact]
    public async Task UnableToResolveState()
    {
        TopicPartition tp1 = new(this.AnyTopicName, 1);

        this.Services.AddScoped<TestOffsetsStorage>();

        using TestObserver observer = new(this, 3);

        var deserializer = new TestDeserializer(observer);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer)
            .WithAssignAndExternalOffsets<TestOffsetsStorage>()
            .WithOptions(options => { options.WithTopicPartitions(tp1); });

        Dictionary<TestEntityKafka, TopicPartitionOffset> entities = await MockCluster.SeedKafka(this, 5, tp1);
        deserializer.WithSuccess(2, entities.Keys.ToArray());

        InvalidOperationException exc = await Assert.ThrowsAsync<InvalidOperationException>(this.RunBackgroundServices);

        exc.Message.ShouldContain(
            "No constructor for type 'Epam.Kafka.PubSub.Tests.Helpers.TestOffsetsStorage' can be instantiated");

        observer.AssertStart();
        observer.AssertStop(exc);
    }
}