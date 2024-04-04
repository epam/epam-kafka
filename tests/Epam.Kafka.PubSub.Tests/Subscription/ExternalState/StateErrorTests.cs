﻿// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Subscription.Options;
using Epam.Kafka.PubSub.Tests.Helpers;
using Epam.Kafka.Tests.Common;

using Microsoft.Extensions.DependencyInjection;

using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.Tests.Subscription.ExternalState;

public class StateErrorTests : TestWithServices, IClassFixture<MockCluster>
{
    private readonly MockCluster _mockCluster;

    public StateErrorTests(ITestOutputHelper output, MockCluster mockCluster) : base(output)
    {
        this._mockCluster = mockCluster ?? throw new ArgumentNullException(nameof(mockCluster));
    }

    [Fact]
    public async Task ErrorOnGet()
    {
        TopicPartition tp3 = new(this.AnyTopicName, 3);

        TestException exception = new();

        using TestObserver observer = new(this, 2);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer)
            .WithAssignAndExternalOffsets<TestOffsetsStorage>().WithOptions(x => x.WithTopicPartitions(tp3));

        offsets.WithGetError(1, exception);
        offsets.WithGetError(2, exception);

        await MockCluster.SeedKafka(this, 1, new TopicPartition(this.AnyTopicName, 0));

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();
        offsets.Verify();

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertStop(exception);

        // iteration 4
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertStop(exception);
    }

    [Fact]
    public async Task ErrorOnSet()
    {
        TopicPartition tp3 = new(this.AnyTopicName, 3);
        TestException exception = new ();

        using TestObserver observer = new(this, 2);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer)
            .WithAssignAndExternalOffsets<TestOffsetsStorage>().WithOptions(x => x.WithTopicPartitions(tp3));

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 5, tp3);

        handler.WithSuccess(1, m1);
        handler.WithSuccess(2, m1);
        deserializer.WithSuccess(1, m1.Keys.ToArray());

        var unset = new TopicPartitionOffset(tp3, Offset.Unset);
        var offset5 = new TopicPartitionOffset(tp3, 5);

        offsets.WithGet(1, unset);
        offsets.WithSetError(1, exception, offset5);
        offsets.WithGet(2, unset);
        offsets.WithSetError(2, exception, offset5);

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
        observer.AssertStop(exception);

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertStop(exception);
    }
}