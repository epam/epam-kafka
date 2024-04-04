// Copyright © 2024 EPAM Systems

using Confluent.Kafka.Admin;
using Epam.Kafka.PubSub.Publication.Pipeline;
using Epam.Kafka.PubSub.Tests.Helpers;
using Epam.Kafka.Tests.Common;

using Microsoft.Extensions.DependencyInjection;

using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.Tests.Publication;

public class PubServiceSuccessTests : TestWithServices, IClassFixture<MockCluster>
{
    private readonly MockCluster _mockCluster;

    public PubServiceSuccessTests(ITestOutputHelper output, MockCluster mockCluster) : base(output)
    {
        this._mockCluster = mockCluster ?? throw new ArgumentNullException(nameof(mockCluster));
    }

    [Fact]
    public async Task Publish()
    {
        TestEntityKafka entity = new();
        PubSub.Publication.TopicMessage<string, TestEntityKafka> message = entity.ToMessage();

        KeyValuePair<string, PubSub.Publication.DeliveryReport> report = message.ToReport(0, this.AnyTopicName);

        using TestObserver observer = new(this, 2);

        const int batchSize = 100;

        TestPublicationHandler handler = new TestPublicationHandler(false, observer)
            .WithBatch(1, batchSize, message).WithReport(1, report)
            .WithBatch(2, batchSize);

        TestSerializer serializer = new TestSerializer(observer).WithSuccess(1, entity);

        this.Services.AddScoped(_ => handler);

        this._mockCluster.LaunchMockCluster(this)
            .AddPublication<string, TestEntityKafka, TestPublicationHandler>(observer.Name, ServiceLifetime.Scoped)
            .WithValueSerializer(_ => serializer)
            .WithOptions(options =>
            {
                options.DefaultTopic = this.AnyTopicName;
                options.BatchSize = batchSize;
            }).WithPartitioner(partitioner => partitioner.Default = (_, _, _, _) => 0);

        await this.RunBackgroundServices();

        serializer.Verify();
        handler.Verify();

        // iteration 1 (one item to publish)

        observer.AssertStart();
        observer.AssertNextActivity("src_read.Start");
        observer.AssertNextActivity("src_read.Stop", 1);
        observer.AssertNextActivity("serialize.Start");
        observer.AssertNextActivity("serialize.Stop");
        observer.AssertNextActivity("produce.Start");
        observer.AssertNextActivity("produce.Stop");
        observer.AssertNextActivity("src_report.Start");
        observer.AssertNextActivity("src_report.Stop");
        observer.AssertStop(PublicationBatchResult.Processed);

        // iteration 2 (empty)
        observer.AssertPubEmpty();
    }

    [Fact]
    public async Task PublishTransaction()
    {
        if (MockCluster.RunningFromGitHubActions)
        {
            return;
        }

        TestEntityKafka entity1 = new();
        PubSub.Publication.TopicMessage<string, TestEntityKafka> message1 = entity1.ToMessage();
        KeyValuePair<string, PubSub.Publication.DeliveryReport> report1 = message1.ToReport(0, this.AnyTopicName);

        TestEntityKafka entity2 = new();
        PubSub.Publication.TopicMessage<string, TestEntityKafka> message2 = entity2.ToMessage();
        KeyValuePair<string, PubSub.Publication.DeliveryReport> report2 = message2.ToReport(1, this.AnyTopicName);

        using TestObserver observer = new(this, 3);

        const int batchSize = 100;

        TestPublicationHandler handler = new TestPublicationHandler(true, observer)
            .WithBatch(1, batchSize, message1).WithReport(1, report1).WithTransaction(1)
            .WithBatch(2, batchSize, message2).WithReport(2, report2).WithTransaction(2)
            .WithBatch(3, batchSize);

        TestSerializer serializer = new TestSerializer(observer).WithSuccess(1, entity1).WithSuccess(2, entity2);

        this.Services.AddScoped(_ => handler);

        this._mockCluster.LaunchMockCluster(this)
            .AddPublication<string, TestEntityKafka, TestPublicationHandler>(observer.Name, ServiceLifetime.Scoped)
            .WithValueSerializer(_ => serializer)
            .WithOptions(options =>
            {
                options.DefaultTopic = this.AnyTopicName;
                options.BatchSize = batchSize;
                options.Producer = MockCluster.TransactionalProducer;
            }).WithPartitioner(partitioner => partitioner.Default = (_, _, _, _) => 0);

        await this.RunBackgroundServices();

        serializer.Verify();
        handler.Verify();

        // iteration 1 (one item to publish, create producer and init transactions)

        observer.AssertStart();
        observer.AssertNextActivity("src_read.Start");
        observer.AssertNextActivity("src_read.Stop", 1);
        observer.AssertNextActivity("serialize.Start");
        observer.AssertNextActivity("serialize.Stop");
        observer.AssertNextActivity("init_transactions.Start");
        observer.AssertNextActivity("init_transactions.Stop");
        observer.AssertNextActivity("begin_transaction.Start");
        observer.AssertNextActivity("begin_transaction.Stop");
        observer.AssertNextActivity("produce.Start");
        observer.AssertNextActivity("produce.Stop");
        observer.AssertNextActivity("src_report.Start");
        observer.AssertNextActivity("src_report.Stop");
        observer.AssertNextActivity("commit_transaction.Start");
        observer.AssertNextActivity("commit_transaction.Stop");
        observer.AssertNextActivity("src_commit.Start");
        observer.AssertNextActivity("src_commit.Stop");
        observer.AssertStop(PublicationBatchResult.Processed);

        // iteration 2 (one item to publish)
        observer.AssertStart();
        observer.AssertNextActivity("src_read.Start");
        observer.AssertNextActivity("src_read.Stop", 1);
        observer.AssertNextActivity("serialize.Start");
        observer.AssertNextActivity("serialize.Stop");
        observer.AssertNextActivity("begin_transaction.Start");
        observer.AssertNextActivity("begin_transaction.Stop");
        observer.AssertNextActivity("produce.Start");
        observer.AssertNextActivity("produce.Stop");
        observer.AssertNextActivity("src_report.Start");
        observer.AssertNextActivity("src_report.Stop");
        observer.AssertNextActivity("commit_transaction.Start");
        observer.AssertNextActivity("commit_transaction.Stop");
        observer.AssertNextActivity("src_commit.Start");
        observer.AssertNextActivity("src_commit.Stop");
        observer.AssertStop(PublicationBatchResult.Processed);

        // iteration 3 (empty)
        observer.AssertPubEmpty();
    }
}