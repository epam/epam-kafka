// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Publication;
using Epam.Kafka.PubSub.Publication.Pipeline;
using Epam.Kafka.PubSub.Tests.Helpers;
using Epam.Kafka.Tests.Common;

using Microsoft.Extensions.DependencyInjection;

using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.Tests.Publication;

public class PubServiceErrorTests : TestWithServices, IClassFixture<MockCluster>
{
    private readonly MockCluster _mockCluster;

    public PubServiceErrorTests(ITestOutputHelper output, MockCluster mockCluster) : base(output)
    {
        this._mockCluster = mockCluster ?? throw new ArgumentNullException(nameof(mockCluster));
    }

    [Fact]
    public async Task HandlerCtorError()
    {
        using TestObserver observer = new(this, 2);

        TestSerializer serializer = new(observer);

        MockCluster.AddMockCluster(this)
            .AddPublication<string, TestEntityKafka, HandlerWithExceptionInConstructor>(observer.Name,
                ServiceLifetime.Scoped)
            .WithValueSerializer(_ => serializer)
            .WithOptions(options =>
            {
                options.DefaultTopic = this.AnyTopicName;
                options.PipelineRetryTimeout = TimeSpan.Zero;
            });

        await this.RunBackgroundServices();

        // pipeline 1
        observer.AssertStart();
        observer.AssertStop(HandlerWithExceptionInConstructor.Exc);

        // pipeline 2
        observer.AssertStart();
        observer.AssertStop(HandlerWithExceptionInConstructor.Exc);
    }

    [Fact]
    public async Task GetBatchError()
    {
        TestException exc1 = new("Test1");
        TestException exc2 = new("Test2");
        TestException exc3 = new("Test3");

        using TestObserver observer = new(this, 3);

        const int batchSize = 100;

        TestPublicationHandler handler = new TestPublicationHandler(false, observer)
            .WithBatch(1, 100, exc1)
            .WithBatch(2, 100, exc2)
            .WithBatch(3, 100, exc3);

        TestSerializer serializer = new(observer);

        this.Services.AddScoped(_ => handler);

        MockCluster.AddMockCluster(this)
            .AddPublication<string, TestEntityKafka, TestPublicationHandler>(observer.Name, ServiceLifetime.Scoped)
            .WithValueSerializer(_ => serializer)
            .WithOptions(options =>
            {
                options.DefaultTopic = this.AnyTopicName;
                options.BatchSize = batchSize;
            });

        await this.RunBackgroundServices();

        handler.Verify();

        // iteration 1

        observer.AssertStart();
        observer.AssertNextActivity("src_read.Start");
        observer.AssertNextActivity("src_read.Stop");
        observer.AssertStop(exc1);

        // iteration 2
        observer.AssertStart();
        observer.AssertNextActivity("src_read.Start");
        observer.AssertNextActivity("src_read.Stop");
        observer.AssertStop(exc2);

        // iteration 3
        observer.AssertStart();
        observer.AssertNextActivity("src_read.Start");
        observer.AssertNextActivity("src_read.Stop");
        observer.AssertStop(exc3);
    }

    [Theory]
    [InlineData(0, 1, true)]
    [InlineData(0, 1, false)]
    [InlineData(1, 0, true)]
    [InlineData(1, 0, false)]
    public async Task SerializerErrorPartialPreprocessing(int successIndex, int errorIndex, bool transaction)
    {
        TestException[] exc = { new("S0"), new("S1") };

        TestEntityKafka[] entity = { new(), new() };

        TopicMessage<string, TestEntityKafka>[] message = entity.Select(x => x.ToMessage()).ToArray();

        KeyValuePair<string, DeliveryReport> report = message[errorIndex].ToReport(Offset.Unset, this.AnyTopicName,
            Partition.Any,
            ErrorCode.Local_ValueSerialization, PersistenceStatus.NotPersisted);

        using TestObserver observer = new(this, 1);

        const int batchSize = 100;

        TestPublicationHandler handler = new TestPublicationHandler(transaction, observer)
            .WithBatch(1, batchSize, message).WithReport(1, report);

        TestSerializer serializer = new(observer);
        serializer.WithSuccess(1, entity[successIndex]).WithError(1, exc[errorIndex], entity[errorIndex]);

        this.Services.AddScoped(_ => handler);

        MockCluster.AddMockCluster(this)
            .AddPublication<string, TestEntityKafka, TestPublicationHandler>(observer.Name, ServiceLifetime.Scoped)
            .WithValueSerializer(_ => serializer)
            .WithOptions(options =>
            {
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

        observer.AssertNextActivity("serialize.Start");
        observer.AssertNextActivity("serialize.Stop");

        observer.AssertNextActivity("src_report.Start");
        observer.AssertNextActivity("src_report.Stop");
        observer.AssertStop(PublicationBatchResult.ProcessedPartial);
    }

    private class HandlerWithExceptionInConstructor : IPublicationHandler<string, TestEntityKafka>
    {
        public HandlerWithExceptionInConstructor()
        {
            throw Exc;
        }

        public static InvalidTimeZoneException Exc { get; } = new("test ctor");

        public void TransactionCommitted(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyCollection<TopicMessage<string, TestEntityKafka>> GetBatch(int count, bool transaction,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void ReportResults(IDictionary<TopicMessage<string, TestEntityKafka>, DeliveryReport> reports,
            DateTimeOffset? transactionEnd, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}