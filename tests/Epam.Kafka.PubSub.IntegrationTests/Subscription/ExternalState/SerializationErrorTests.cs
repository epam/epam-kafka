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

[Collection(SubscribeTests.Name)]
public class SerializationErrorTests : TestWithServices, IClassFixture<MockCluster>
{
    private readonly MockCluster _mockCluster;

    public SerializationErrorTests(ITestOutputHelper output, MockCluster mockCluster) : base(output)
    {
        this._mockCluster = mockCluster ?? throw new ArgumentNullException(nameof(mockCluster));
    }

    [Fact]
    public async Task SinglePartitionAtBeginning()
    {
        TopicPartition tp3 = new(this.AnyTopicName, 2);

        TestException exc = new();
        using TestObserver observer = new(this, 3);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithAssignAndExternalOffsets<TestOffsetsStorage>().WithValueDeserializer(_ => deserializer)
            .WithOptions(x => x.WithTopicPartitions(tp3));

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 5, tp3);

        deserializer.WithError(1, exc, m1.Keys.First());
        deserializer.WithError(2, exc, m1.Keys.First());

        TopicPartitionOffset unset = new (tp3, Offset.Unset);
        TopicPartitionOffset autoReset = new (tp3, 0);
        offsets.WithGet(1, unset);
        offsets.WithSet(1, autoReset);
        offsets.WithGet(2, autoReset);

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();

        // iteration 1
        observer.AssertStart();
        observer.AssertAssign(true);
        observer.AssertRead();
        observer.AssertStop<ConsumeException>("Value deserialization error");

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead();
        observer.AssertStop<ConsumeException>("Value deserialization error");
    }

    [Fact]
    public async Task SinglePartitionInTheMiddleOfBatch()
    {
        TopicPartition tp3 = new(this.AnyTopicName, 3);

        TestException exc = new();
        using TestObserver observer = new(this, 2);

        var handler = new TestSubscriptionHandler(observer);
        var offsets = new TestOffsetsStorage(observer);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        observer.CreateDefaultSubscription(this._mockCluster).WithAssignAndExternalOffsets<TestOffsetsStorage>().WithValueDeserializer(_ => deserializer)
            .WithOptions(x => x.WithTopicPartitions(tp3));

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 5, tp3);

        deserializer.WithSuccess(1, m1.Keys.ElementAt(0));
        deserializer.WithError(1, exc, m1.Keys.ElementAt(1));
        deserializer.WithError(2, exc, m1.Keys.ElementAt(1));

        handler.WithSuccess(1, m1.Take(1));

        TopicPartitionOffset unset = new (tp3, Offset.Unset);
        TopicPartitionOffset autoReset = new (tp3, 0);
        TopicPartitionOffset offset1 = new (tp3, 1);

        offsets.WithGet(1, unset);
        offsets.WithSet(1, autoReset);
        offsets.WithSetAndGetForNextIteration(1, offset1);

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();

        // iteration 1 process deserialized items before error
        observer.AssertStart();
        observer.AssertAssign(true);
        observer.AssertRead(1);
        observer.AssertProcess();
        observer.AssertCommitExternal();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead();
        observer.AssertStop<ConsumeException>("Value deserialization error");
    }
}