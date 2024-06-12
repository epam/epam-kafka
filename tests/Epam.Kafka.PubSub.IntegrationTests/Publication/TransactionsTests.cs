// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.IntegrationTests.Publication;

public class TransactionsTests : TestWithServices, IClassFixture<MockCluster>
{
    private readonly MockCluster _mockCluster;

    public TransactionsTests(ITestOutputHelper output, MockCluster mockCluster) : base(output)
    {
        this._mockCluster = mockCluster ?? throw new ArgumentNullException(nameof(mockCluster));
    }

    [Fact]
    public async Task PublishTransaction()
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
}