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
        TopicPartition tp1 = new(this.AnyTopicName, 1);

        using TestObserver observer = new(this, 3);

        var handler = new TestConversionHandler(observer);
        var offsets = new TestOffsetsStorage(observer);
        var deserializer = new TestDeserializer(observer);
        var serializer = new TestSerializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        KafkaBuilder kafkaBuilder = this._mockCluster.LaunchMockCluster(observer.Test);

        kafkaBuilder.WithDefaultConsumer(observer);

        kafkaBuilder
            .AddReplication<string, TestEntityKafka, string, TestEntityKafka, TestConversionHandler>(observer.Name, ServiceLifetime.Scoped, valueSerializer: _ => serializer)
            .WithValueDeserializer(_ => deserializer)
            .WithAssignAndExternalOffsets<TestOffsetsStorage>()
            .WithOptions(options =>
            {
                options.BatchSize = 5;
                options.WithTopicPartitions(tp1);
                options.Replication.DefaultTopic = $"{observer.Name}.pub";
            });

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 5, tp1);
        Dictionary<TestEntityKafka, TopicPartitionOffset> m2 = await MockCluster.SeedKafka(this, 5, tp1);

        handler.WithSuccess(1, m1.Keys.ToArray());
        handler.WithSuccess(2, m2.Keys.ToArray());

        deserializer.WithSuccess(1, m1.Keys.ToArray());
        deserializer.WithSuccess(2, m2.Keys.ToArray());

        serializer.WithSuccess(1, m1.Keys.ToArray());
        serializer.WithSuccess(2, m2.Keys.ToArray());

        offsets.WithGet(1, new TopicPartitionOffset(tp1, Offset.Unset));
        offsets.WithSet(1, new TopicPartitionOffset(tp1, 0)); // auto reset
        offsets.WithSetAndGetForNextIteration(1, new TopicPartitionOffset(tp1, 5));
        offsets.WithSetAndGetForNextIteration(2, new TopicPartitionOffset(tp1, 10));

        await this.RunBackgroundServices();

        deserializer.Verify();
        serializer.Verify();
        handler.Verify();

        // iteration 1
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(5);
        observer.AssertNextActivity("process.Start");
        observer.AssertNextActivity("serialize.Start");
        observer.AssertNextActivity("serialize.Stop");
        observer.AssertNextActivity("produce.Start");
        observer.AssertNextActivity("produce.Stop");
        observer.AssertNextActivity("process.Stop");
        observer.AssertCommitExternal();
        observer.AssertStop(SubscriptionBatchResult.Processed);

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
        observer.AssertCommitExternal();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 3
        observer.AssertSubEmpty();
    }
    
    [Fact]
    public async Task TransactionSameCluster()
    {
        TopicPartition tp1 = new(this.AnyTopicName, 1);

        using TestObserver observer = new(this, 3);

        var handler = new TestConversionHandler(observer);
        var offsets = new TestOffsetsStorage(observer);
        var deserializer = new TestDeserializer(observer);
        var serializer = new TestSerializer(observer);

        this.Services.AddScoped(_ => handler);
        this.Services.AddScoped(_ => offsets);

        KafkaBuilder kafkaBuilder = this._mockCluster.LaunchMockCluster(observer.Test);

        kafkaBuilder.WithDefaultConsumer(observer);

        kafkaBuilder.WithProducerConfig(MockCluster.TransactionalProducer)
            .Configure(x =>
            {
                x.ProducerConfig.TransactionalId = nameof(this.TransactionSameCluster);
            });

        kafkaBuilder
            .AddReplication<string, TestEntityKafka, string, TestEntityKafka, TestConversionHandler>(observer.Name, ServiceLifetime.Scoped, valueSerializer: _ => serializer)
            .WithValueDeserializer(_ => deserializer)
            .WithAssignAndExternalOffsets<TestOffsetsStorage>()
            .WithOptions(options =>
            {
                options.BatchSize = 5;
                options.WithTopicPartitions(tp1);
                options.Replication.DefaultTopic = $"{observer.Name}.pub";
                options.Replication.Producer = MockCluster.TransactionalProducer;
            });

        Dictionary<TestEntityKafka, TopicPartitionOffset> m1 = await MockCluster.SeedKafka(this, 5, tp1);
        Dictionary<TestEntityKafka, TopicPartitionOffset> m2 = await MockCluster.SeedKafka(this, 5, tp1);

        handler.WithSuccess(1, m1.Keys.ToArray());
        handler.WithSuccess(2, m2.Keys.ToArray());

        deserializer.WithSuccess(1, m1.Keys.ToArray());
        deserializer.WithSuccess(2, m2.Keys.ToArray());

        serializer.WithSuccess(1, m1.Keys.ToArray());
        serializer.WithSuccess(2, m2.Keys.ToArray());

        offsets.WithGet(1, new TopicPartitionOffset(tp1, Offset.Unset));
        offsets.WithSet(1, new TopicPartitionOffset(tp1, 0)); // auto reset
        offsets.WithSetAndGetForNextIteration(1, new TopicPartitionOffset(tp1, 5));
        offsets.WithSetAndGetForNextIteration(2, new TopicPartitionOffset(tp1, 10));

        await this.RunBackgroundServices();

        deserializer.Verify();
        serializer.Verify();
        handler.Verify();

        // iteration 1
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(5);
        observer.AssertNextActivity("process.Start");
        observer.AssertNextActivity("serialize.Start");
        observer.AssertNextActivity("serialize.Stop");
        observer.AssertNextActivity("init_transactions.Start");
        observer.AssertNextActivity("init_transactions.Stop");
        observer.AssertNextActivity("begin_transaction.Start");
        observer.AssertNextActivity("begin_transaction.Stop");
        observer.AssertNextActivity("produce.Start");
        observer.AssertNextActivity("produce.Stop");
        observer.AssertNextActivity("commit_transaction.Start");
        observer.AssertNextActivity("commit_transaction.Stop");
        observer.AssertNextActivity("process.Stop");
        observer.AssertCommitExternal();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(5);
        observer.AssertNextActivity("process.Start");
        observer.AssertNextActivity("serialize.Start");
        observer.AssertNextActivity("serialize.Stop");
        observer.AssertNextActivity("begin_transaction.Start");
        observer.AssertNextActivity("begin_transaction.Stop");
        observer.AssertNextActivity("produce.Start");
        observer.AssertNextActivity("produce.Stop");
        observer.AssertNextActivity("commit_transaction.Start");
        observer.AssertNextActivity("commit_transaction.Stop");
        observer.AssertNextActivity("process.Stop");
        observer.AssertCommitExternal();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 3
        observer.AssertSubEmpty();
    }
}