﻿// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.PubSub.Tests.Helpers;
using Epam.Kafka.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.IntegrationTests.Subscription.InternalState;

[Collection(SubscribeTests.Name)]
public class ReplicationTests : TestWithServices, IClassFixture<MockCluster>
{
    private readonly MockCluster _mockCluster;

    public ReplicationTests(ITestOutputHelper output, MockCluster mockCluster) : base(output)
    {
        this._mockCluster = mockCluster ?? throw new ArgumentNullException(nameof(mockCluster));
    }

    [Fact]
    public async Task OnePartitionTwoBatches()
    {
        TopicPartition tp3 = new(this.AnyTopicName, 3);

        using TestObserver observer = new(this, 4);

        var handler = new TestConversionHandler(observer);
        var deserializer = new TestDeserializer(observer);
        var serializer = new TestSerializer(observer);

        this.Services.AddScoped(_ => handler);

        KafkaBuilder kafkaBuilder = this._mockCluster.LaunchMockCluster(observer.Test);

        // dedicated unique group for each test to avoid Group re-balance in progress exception on parallel test runs.
        kafkaBuilder.WithConsumerConfig(MockCluster.DefaultConsumer)
            .Configure(x =>
            {
                x.ConsumerConfig.GroupId = observer.Name;
                x.ConsumerConfig.SessionTimeoutMs = 10_000;
                x.ConsumerConfig.AutoOffsetReset = AutoOffsetReset.Earliest;
                x.ConsumerConfig.SetCancellationDelayMaxMs(2000);
                x.ConsumerConfig.PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky;
            });

        var pubsub = kafkaBuilder
            .AddReplication<string, TestEntityKafka, string, TestEntityKafka, TestConversionHandler>(observer.Name, ServiceLifetime.Scoped,valueSerializer: _ => serializer)
            .WithValueDeserializer(_ => deserializer)
            .WithOptions(options =>
            {
                options.BatchSize = 5;
                options.Topics = observer.Test.AnyTopicName;
                options.BatchNotAssignedTimeout = TimeSpan.FromSeconds(1);
                options.BatchEmptyTimeout = TimeSpan.Zero;
                options.PipelineRetryTimeout = TimeSpan.Zero;
                options.BatchPausedTimeout = TimeSpan.Zero;
                options.BatchRetryMaxTimeout = TimeSpan.Zero;
                options.Replication.DefaultTopic = $"{observer.Name}.pub";
            });

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 5, tp3);
        Dictionary<TestEntityKafka, TopicPartitionOffset> m2 = await MockCluster.SeedKafka(this, 5, tp3);

        deserializer.WithSuccess(2, m1.Keys.ToArray());
        serializer.WithSuccess(2, m1.Keys.ToArray());
        handler.WithSuccess(2, entities: m1.Keys.ToArray());

        deserializer.WithSuccess(3, m2.Keys.ToArray());
        serializer.WithSuccess(3, m2.Keys.ToArray());
        handler.WithSuccess(3, entities: m2.Keys.ToArray());

        await this.RunBackgroundServices();

        deserializer.Verify();
        serializer.Verify();
        handler.Verify();

        // iteration 1
        observer.AssertSubNotAssigned();

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(5);
        observer.AssertNextActivity("process.Start");
        observer.AssertNextActivity("serialize.Start");
        observer.AssertNextActivity("serialize.Stop");
        observer.AssertNextActivity("produce.Start");
        observer.AssertNextActivity("produce.Stop");
        observer.AssertNextActivity("process.Stop");
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 3
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(5);
        observer.AssertNextActivity("process.Start");
        observer.AssertNextActivity("serialize.Start");
        observer.AssertNextActivity("serialize.Stop");
        observer.AssertNextActivity("produce.Start");
        observer.AssertNextActivity("produce.Stop");
        observer.AssertNextActivity("process.Stop");
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 4
        observer.AssertSubEmpty();
    }
}