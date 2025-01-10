// Copyright © 2024 EPAM Systems

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

        kafkaBuilder.WithDefaultConsumer(observer);

        kafkaBuilder
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

    [Fact]
    public async Task ConvertEmpty()
    {
        TopicPartition tp3 = new(this.AnyTopicName, 3);

        using TestObserver observer = new(this, 4);

        var handler = new TestConversionHandler(observer);
        var deserializer = new TestDeserializer(observer);
        var serializer = new TestSerializer(observer);

        this.Services.AddScoped(_ => handler);

        KafkaBuilder kafkaBuilder = this._mockCluster.LaunchMockCluster(observer.Test);

        kafkaBuilder.WithDefaultConsumer(observer);

        kafkaBuilder
            .AddReplication<string, TestEntityKafka, string, TestEntityKafka, TestConversionHandler>(observer.Name, ServiceLifetime.Scoped, valueSerializer: _ => serializer)
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
        //serializer.WithSuccess(2, m1.Keys.ToArray());
        handler.WithEmpty(2, entities: m1.Keys.ToArray());

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

    [Fact]
    public async Task TransactionSameCluster()
    {
        TopicPartition tp3 = new(this.AnyTopicName, 3);

        using TestObserver observer = new(this, 4);

        var handler = new TestConversionHandler(observer);
        var deserializer = new TestDeserializer(observer);
        var serializer = new TestSerializer(observer);

        this.Services.AddScoped(_ => handler);

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
                options.Replication.Producer = MockCluster.TransactionalProducer;
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
        observer.AssertNextActivity("init_transactions.Start");
        observer.AssertNextActivity("init_transactions.Stop");
        observer.AssertNextActivity("begin_transaction.Start");
        observer.AssertNextActivity("begin_transaction.Stop");
        observer.AssertNextActivity("produce.Start");
        observer.AssertNextActivity("produce.Stop");
        observer.AssertNextActivity("offsets_transaction.Start");
        observer.AssertNextActivity("offsets_transaction.Stop");
        observer.AssertNextActivity("commit_transaction.Start");
        observer.AssertNextActivity("commit_transaction.Stop");
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
        observer.AssertNextActivity("begin_transaction.Start");
        observer.AssertNextActivity("begin_transaction.Stop");
        observer.AssertNextActivity("produce.Start");
        observer.AssertNextActivity("produce.Stop");
        observer.AssertNextActivity("offsets_transaction.Start");
        observer.AssertNextActivity("offsets_transaction.Stop");
        observer.AssertNextActivity("commit_transaction.Start");
        observer.AssertNextActivity("commit_transaction.Stop");
        observer.AssertNextActivity("process.Stop");
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 4
        observer.AssertSubEmpty();
    }

    [Fact]
    public async Task TransactionDifferentCluster()
    {
        TopicPartition tp3 = new(this.AnyTopicName, 3);

        using TestObserver observer = new(this, 4);

        var handler = new TestConversionHandler(observer);
        var deserializer = new TestDeserializer(observer);
        var serializer = new TestSerializer(observer);

        this.Services.AddScoped(_ => handler);

        KafkaBuilder kafkaBuilder = this._mockCluster.LaunchMockCluster(observer.Test);

        kafkaBuilder.WithDefaultConsumer(observer);

        const string testMock = "testMock";
        kafkaBuilder.WithClusterConfig(testMock).Configure(options =>
        {
            options.ClientConfig.AllowAutoCreateTopics = true;
            options.ClientConfig.BootstrapServers = "localhost:9092";
            options.ClientConfig.Set("test.mock.num.brokers", "1");
        });

        kafkaBuilder.WithProducerConfig(MockCluster.TransactionalProducer)
            .Configure(x =>
            {
                x.ProducerConfig.TransactionalId = nameof(this.TransactionSameCluster);
                x.ProducerConfig.AllowAutoCreateTopics = true;
            });

        kafkaBuilder
            .AddReplication<string, TestEntityKafka, string, TestEntityKafka, TestConversionHandler>(observer.Name, ServiceLifetime.Scoped, valueSerializer: _ => serializer)
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
                options.Replication.Producer = MockCluster.TransactionalProducer;
                options.Replication.Cluster = testMock;
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
        observer.AssertNextActivity("init_transactions.Start");
        observer.AssertNextActivity("init_transactions.Stop");
        observer.AssertNextActivity("begin_transaction.Start");
        observer.AssertNextActivity("begin_transaction.Stop");
        observer.AssertNextActivity("produce.Start");
        observer.AssertNextActivity("produce.Stop");
        observer.AssertNextActivity("commit_transaction.Start");
        observer.AssertNextActivity("commit_transaction.Stop");
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
        observer.AssertNextActivity("begin_transaction.Start");
        observer.AssertNextActivity("begin_transaction.Stop");
        observer.AssertNextActivity("produce.Start");
        observer.AssertNextActivity("produce.Stop");
        observer.AssertNextActivity("commit_transaction.Start");
        observer.AssertNextActivity("commit_transaction.Stop");
        observer.AssertNextActivity("process.Stop");
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 4
        observer.AssertSubEmpty();
    }
}