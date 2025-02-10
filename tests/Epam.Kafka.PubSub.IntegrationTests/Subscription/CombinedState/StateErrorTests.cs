// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka.PubSub.Tests.Helpers;
using Epam.Kafka.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.IntegrationTests.Subscription.CombinedState;

[Collection(SubscribeTests.Name)]
public class StateErrorTests : TestWithServices
{
    private readonly MockCluster _mockCluster;

    public StateErrorTests(ITestOutputHelper output, MockCluster mockCluster) : base(output)
    {
        this._mockCluster = mockCluster ?? throw new ArgumentNullException(nameof(mockCluster));
    }

    [Fact]
    public async Task ErrorOnSet()
    {
        TopicPartition tp3 = new(this.AnyTopicName, 3);
        TestException exception = new ();

        using TestObserver observer = new(this, 3);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer, 0, 1, 2);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer)
            .WithSubscribeAndExternalOffsets<TestOffsetsStorage>();

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 5, tp3);

        handler.WithSuccess(2, m1);
        handler.WithSuccess(3, m1);
        deserializer.WithSuccess(2, m1.Keys.ToArray());

        var unset = new TopicPartitionOffset(tp3, Offset.Unset);
        var autoReset = new TopicPartitionOffset(tp3, 0);
        var offset5 = new TopicPartitionOffset(tp3, 5);

        offsets.WithGet(2, unset);
        offsets.WithSet(2, autoReset);
        offsets.WithSetError(2, exception, offset5);
        offsets.WithGet(3, autoReset);
        offsets.WithSetError(3, exception, offset5);

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
        observer.AssertStop(exception);

        // iteration 3
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(0);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertStop(exception);
    }

    [Fact]
    public async Task ErrorOnGet()
    {
        TestException exception = new();

        using TestObserver observer = new(this, 4);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer)
            .WithSubscribeAndExternalOffsets<TestOffsetsStorage>();

        offsets.WithGetError(2, exception);
        offsets.WithGetError(4, exception);

        await MockCluster.SeedKafka(this, 1, new TopicPartition(this.AnyTopicName, 0));

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();
        offsets.Verify();

        // iteration 1
        observer.AssertSubNotAssigned();

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(0);
        observer.AssertStop(exception);

        // iteration 3
        observer.AssertSubNotAssigned();

        // iteration 4
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(0);
        observer.AssertStop(exception);
    }
}