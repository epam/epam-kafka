﻿// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.PubSub.Tests.Helpers;
using Epam.Kafka.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.Tests.Subscription.InternalState;

[Collection(SubscribeTests.Name)]
public class HandlerErrorTests : TestWithServices
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

        using TestObserver observer = new(this, 8);

        var handler = new TestSubscriptionHandler(observer);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer).WithOptions(x =>
            {
                x.BatchSize = 6;
            });

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 6, tp3);
        Dictionary<TestEntityKafka, TopicPartitionOffset> m2 = await MockCluster.SeedKafka(this, 4, tp3);

        handler.WithError(2, exception, m1);
        handler.WithError(3, exception, m1);
        handler.WithError(4, exception, m1.Take(3));
        handler.WithError(5, exception, m1.Take(1));
        handler.WithSuccess(6, m1.Take(1));
        handler.WithSuccess(7, m1.Skip(1));
        handler.WithSuccess(8, m2);

        deserializer.WithSuccess(2, m1.Keys.ToArray());
        deserializer.WithSuccess(8, m2.Keys.ToArray());

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();

        // iteration 1
        observer.AssertSubNotAssigned();

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(6);
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
        observer.AssertStop(exception);

        // iteration 6
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertProcess();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 7
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertProcess();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 8
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(4);
        observer.AssertProcess();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);
    }
}