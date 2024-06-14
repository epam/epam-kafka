// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.IntegrationTests.Publication;

[Collection(SubscribeTests.Name)]
public class TransactionsTests : TestWithServices, IClassFixture<MockCluster>
{
    private readonly MockCluster _mockCluster;

    public TransactionsTests(ITestOutputHelper output, MockCluster mockCluster) : base(output)
    {
        this._mockCluster = mockCluster ?? throw new ArgumentNullException(nameof(mockCluster));
    }

    [Fact]
    public async Task DifferentIds()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(5);

        this._mockCluster.LaunchMockCluster(this);

        using var p1 = this.KafkaFactory.CreateProducer<string,string>(new ProducerConfig { TransactionalId = "test-1" });

        await Task.Delay(100);

        using var p2 = this.KafkaFactory.CreateProducer<string,string>(new ProducerConfig { TransactionalId = "test-2" });

        p1.InitTransactions(timeout);
        this.Output.WriteLine("InitTransactions 1");

        p1.BeginTransaction();
        this.Output.WriteLine("BeginTransaction 1");

        var r1 = await p1.ProduceAsync("qwe-tr-1", new Message<string, string> { Key = "k1", Value = "v1" });
        this.Output.WriteLine($"{r1.Status:G} {r1.TopicPartitionOffset}");

        p2.InitTransactions(timeout);
        this.Output.WriteLine("InitTransactions 2");

        p2.BeginTransaction();
        this.Output.WriteLine("BeginTransaction 2");

        p1.CommitTransaction(timeout);
        this.Output.WriteLine("CommitTransaction 1");

        var r2 = await p2.ProduceAsync("qwe-tr-2", new Message<string, string> { Key = "k2", Value = "v2" });
        this.Output.WriteLine($"{r2.Status:G} {r2.TopicPartitionOffset}");

        p2.CommitTransaction(timeout);
        this.Output.WriteLine("CommitTransaction 2");
    }

    [Fact]
    public async Task SameIdFencedBeforeSend()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(5);

        this._mockCluster.LaunchMockCluster(this);

        using var p1 = this.KafkaFactory.CreateProducer<string, string>(new ProducerConfig { TransactionalId = "test-123" });

        await Task.Delay(100);

        using var p2 = this.KafkaFactory.CreateProducer<string, string>(new ProducerConfig { TransactionalId = "test-123" });

        p1.InitTransactions(timeout);
        this.Output.WriteLine("InitTransactions 1");

        p2.InitTransactions(timeout);
        this.Output.WriteLine("InitTransactions 2");

        p1.BeginTransaction();
        this.Output.WriteLine("BeginTransaction 1");

        var exc = await Assert.ThrowsAsync<ProduceException<string, string>>(async () =>
            await p1.ProduceAsync("qwe-tr-1", new Message<string, string> { Key = "k1", Value = "v1" }));
        this.Output.WriteLine($"{exc.Error.Code:G} {exc.DeliveryResult.Status:G} {exc.Message}");

        p2.BeginTransaction();
        this.Output.WriteLine("BeginTransaction 2");

        var exc2 = Assert.Throws<KafkaException>( () => p1.CommitTransaction(timeout));
        this.Output.WriteLine($"{exc2.Error.Code:G} {exc2.Message}");

        var exc3 = Assert.Throws<KafkaException>(() => p1.AbortTransaction(timeout));
        this.Output.WriteLine($"{exc3.Error.Code:G} {exc3.Message}");

        var r2 = await p2.ProduceAsync("qwe-tr-2", new Message<string, string> { Key = "k2", Value = "v2" });
        this.Output.WriteLine($"{r2.Status:G} {r2.TopicPartitionOffset}");

        p2.CommitTransaction(timeout);
        this.Output.WriteLine("CommitTransaction 2");
    }

    [Fact]
    public async Task SameIdFencedBeforeCommit()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(5);

        this._mockCluster.LaunchMockCluster(this);

        using var p1 = this.KafkaFactory.CreateProducer<string, string>(new ProducerConfig { TransactionalId = "test-123" });

        await Task.Delay(100);

        using var p2 = this.KafkaFactory.CreateProducer<string, string>(new ProducerConfig { TransactionalId = "test-123" });

        p1.InitTransactions(timeout);
        this.Output.WriteLine("InitTransactions 1");

        p1.BeginTransaction();
        this.Output.WriteLine("BeginTransaction 1");

        var r1 = await p1.ProduceAsync("qwe-tr-1", new Message<string, string> { Key = "k1", Value = "v1" });
        this.Output.WriteLine($"{r1.Status:G} {r1.TopicPartitionOffset}");

        p2.InitTransactions(timeout);
        this.Output.WriteLine("InitTransactions 2");

        var exc2 = Assert.Throws<KafkaException>(() => p1.CommitTransaction(timeout));
        this.Output.WriteLine($"{exc2.Error.Code:G} {exc2.Message}");

        p2.BeginTransaction();
        this.Output.WriteLine("BeginTransaction 2");

        var r2 = await p2.ProduceAsync("qwe-tr-2", new Message<string, string> { Key = "k2", Value = "v2" });
        this.Output.WriteLine($"{r2.Status:G} {r2.TopicPartitionOffset}");

        p2.CommitTransaction(timeout);
        this.Output.WriteLine("CommitTransaction 2");
    }
}