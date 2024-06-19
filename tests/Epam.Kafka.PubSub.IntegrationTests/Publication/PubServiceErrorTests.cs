// Copyright © 2024 EPAM Systems

using Castle.Components.DictionaryAdapter;
using Confluent.Kafka;
using Epam.Kafka.PubSub.Publication;
using Epam.Kafka.PubSub.Publication.Pipeline;
using Epam.Kafka.PubSub.Tests.Helpers;
using Epam.Kafka.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.IntegrationTests.Publication;

[Collection(SubscribeTests.Name)]
public class PubServiceErrorTests : TestWithServices, IClassFixture<MockCluster>
{
    private readonly MockCluster _mockCluster;

    public PubServiceErrorTests(ITestOutputHelper output, MockCluster mockCluster) : base(output)
    {
        this._mockCluster = mockCluster ?? throw new ArgumentNullException(nameof(mockCluster));
    }

    [Theory]
    [InlineData(0, 1, true)]
    [InlineData(0, 1, false)]
    [InlineData(1, 0, true)]
    [InlineData(1, 0, false)]
    public async Task SerializerErrorPartial(int successIndex, int errorIndex, bool transaction)
    {
        TestException[] exc = { new("S0"), new("S1") };

        TestEntityKafka[] entity = { new(), new() };

        TopicMessage<string, TestEntityKafka>[] message = entity.Select(x => x.ToMessage()).ToArray();

        List<KeyValuePair<string, DeliveryReport>> report = new EditableList<KeyValuePair<string, DeliveryReport>>
        {
            message[errorIndex].ToReport(Offset.Unset, this.AnyTopicName, Partition.Any,
                ErrorCode.Local_ValueSerialization, PersistenceStatus.NotPersisted)
        };

        if (successIndex == 0)
        {
            report.Add(message[successIndex].ToReport(0, this.AnyTopicName));
        }

        using TestObserver observer = new(this, 1);

        const int batchSize = 100;

        TestPublicationHandler handler = new TestPublicationHandler(transaction, observer)
            .WithBatch(1, batchSize, message).WithReport(1, report.ToArray());

        TestSerializer serializer = new(observer);
        serializer.WithSuccess(1, entity[successIndex]).WithError(1, exc[errorIndex], entity[errorIndex]);

        this.Services.AddScoped(_ => handler);

        this._mockCluster.LaunchMockCluster(this)
            .AddPublication<string, TestEntityKafka, TestPublicationHandler>(observer.Name, ServiceLifetime.Scoped)
            .WithValueSerializer(_ => serializer)
            .WithOptions(options =>
            {
                options.SerializationPreprocessor = false;
                options.DefaultTopic = this.AnyTopicName;
                options.BatchSize = batchSize;
                if (transaction)
                {
                    options.Producer = MockCluster.TransactionalProducer;
                }
            }).WithPartitioner(partitioner => partitioner.Default = (_, _, _, _) => 0);

        await this.RunBackgroundServices();

        handler.Verify();

        // iteration 1

        observer.AssertStart();
        observer.AssertNextActivity("src_read.Start");
        observer.AssertNextActivity("src_read.Stop", 2);
        if (transaction)
        {
            observer.AssertNextActivity("init_transactions.Start");
            observer.AssertNextActivity("init_transactions.Stop");
            observer.AssertNextActivity("begin_transaction.Start");
            observer.AssertNextActivity("begin_transaction.Stop");
        }

        observer.AssertNextActivity("produce.Start");
        observer.AssertNextActivity("produce.Stop");
        observer.AssertNextActivity("src_report.Start");
        observer.AssertNextActivity("src_report.Stop");
        if (transaction)
        {
            observer.AssertNextActivity("abort_transaction.Start");
            observer.AssertNextActivity("abort_transaction.Stop");
        }

        observer.AssertStop(PublicationBatchResult.ProcessedPartial);
    }
}