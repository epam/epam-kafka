﻿// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Subscription;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.PubSub.Tests.Helpers;
using Epam.Kafka.Tests.Common;

using Microsoft.Extensions.DependencyInjection;

using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.IntegrationTests.Subscription.CombinedState;

[Collection(SubscribeTests.Name)]
public class SerializationErrorTests : TestWithServices
{
    private readonly MockCluster _mockCluster;

    public SerializationErrorTests(ITestOutputHelper output, MockCluster mockCluster) : base(output)
    {
        this._mockCluster = mockCluster ?? throw new ArgumentNullException(nameof(mockCluster));
    }

    [Fact]
    public async Task SinglePartitionAtBeginning()
    {
        TopicPartition tp3 = new(this.AnyTopicName, 3);

        TestException exc = new();
        using TestObserver observer = new(this, 5);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer, 0, 1, 2);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithSubscribeAndExternalOffsets<TestOffsetsStorage>().WithValueDeserializer(_ => deserializer)
            .WithOptions(x =>
            {
                x.BatchNotAssignedTimeout = TimeSpan.FromSeconds(10);
                x.BatchRetryCount = 1;
            });

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 5, tp3);

        deserializer.WithError(2, exc, m1.Keys.First());
        deserializer.WithError(3, exc, m1.Keys.First());
        deserializer.WithError(5, exc, m1.Keys.First());

        TopicPartitionOffset unset = new(tp3, Offset.Unset);
        TopicPartitionOffset autoReset = new(tp3, 0);
        offsets.WithGet(2, unset);
        offsets.WithSetAndGetForNextIteration(2, autoReset);
        offsets.WithGet(5, autoReset);

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();

        // iteration 1
        observer.AssertSubNotAssigned();

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(null, true);
        observer.AssertStop<ConsumeException>("Value deserialization error");

        // iteration 3
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead();
        observer.AssertStop<ConsumeException>("Value deserialization error");

        // iteration 4 partition not assigned until 6 sec session timeout elapsed
        observer.AssertSubNotAssigned();

        // iteration 5
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(null, true);
        observer.AssertStop<ConsumeException>("Value deserialization error");
    }

    [Fact]
    public async Task PauseAndResumeAtError()
    {
        TopicPartition tp3 = new(this.AnyTopicName, 3);

        TestException exc = new();
        using TestObserver observer = new(this, 5);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer, 0, 1, 2);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithSubscribeAndExternalOffsets<TestOffsetsStorage>().WithValueDeserializer(_ => deserializer)
            .WithOptions(x => x.BatchNotAssignedTimeout = TimeSpan.FromSeconds(10));

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 5, tp3);

        deserializer.WithSuccess(2, m1.Keys.ElementAt(0));
        deserializer.WithError(2, exc, m1.Keys.ElementAt(1));
        deserializer.WithError(3, exc, m1.Keys.ElementAt(1));

        handler.WithSuccess(2, m1.Take(1));

        TopicPartitionOffset unset = new(tp3, Offset.Unset);
        TopicPartitionOffset autoReset = new(tp3, 0);
        TopicPartitionOffset error = new(tp3, 1);
        TopicPartitionOffset paused = new(tp3, ExternalOffset.Paused);

        offsets.WithGet(2, unset);
        offsets.WithSet(2, autoReset);
        offsets.WithSetAndGetForNextIteration(2, error);
        offsets.WithGet(4, paused);
        offsets.WithGet(5, error);

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();

        // iteration 1
        observer.AssertSubNotAssigned();

        // iteration 2 process deserialized items before error
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(1, true);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 3
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead();
        observer.AssertStop<ConsumeException>("Value deserialization error");

        // iteration 4
        observer.AssertSubPaused();

        // iteration 5
        observer.AssertStart();
        observer.AssertAssign(true);
        observer.AssertRead();
        observer.AssertStop<ConsumeException>("Value deserialization error");
    }
}