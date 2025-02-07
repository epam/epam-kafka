// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.Tests.Common;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.Tests;

public class MetricsTests : TestWithServices
{
    public MetricsTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task CreateDefaultClientWithMetrics()
    {
        MockCluster.AddMockCluster(this).WithClusterConfig(MockCluster.ClusterName)
            .Configure(x => x.ClientConfig.StatisticsIntervalMs = 100);

        using MeterHelper ml = new(Statistics.TopLevelMeterName);
        ml.RecordObservableInstruments();
        ml.Results.Count.ShouldBe(0);

        using IClient c1 = this.KafkaFactory.GetOrCreateClient();
        Assert.NotNull(c1);

        await Task.Delay(1000);
        ml.RecordObservableInstruments(this.Output);

        ml.Results.Count.ShouldBe(4);
    }

    [Fact]
    public async Task ConsumerTopParMetricsAssign()
    {
        this.Services.AddKafka(false).WithTestMockCluster(MockCluster.ClusterName);

        using MeterHelper ml = new(Statistics.TopicPartitionMeterName);
        ml.RecordObservableInstruments();
        ml.Results.Count.ShouldBe(0);

        this.ServiceProvider.GetRequiredKeyedService<TestMockCluster>(MockCluster.ClusterName).SeedTopic("test1",
            new Message<byte[], byte[]?> { Key = new byte[] { 0 } },
            new Message<byte[], byte[]?> { Key = new byte[] { 1 } },
            new Message<byte[], byte[]?> { Key = new byte[] { 2 } });

        using IConsumer<Ignore, Ignore> consumer =
            this.KafkaFactory.CreateConsumer<Ignore, Ignore>(new ConsumerConfig
            {
                GroupId = "qwe",
                StatisticsIntervalMs = 100,
                AutoOffsetReset = AutoOffsetReset.Earliest
            }, MockCluster.ClusterName);

        // No assigned topic partitions
        await Task.Delay(200);
        ml.RecordObservableInstruments();
        ml.Results.Count.ShouldBe(0);

        // One 1 of 4 assigned
        TopicPartition tp = new("test1", 1);
        consumer.Assign(tp);

        consumer.Commit(Enumerable.Repeat(new TopicPartitionOffset(tp, 0), 1));

        consumer.Consume(200);
        await Task.Delay(3000);
        consumer.Consume(200);

        ml.RecordObservableInstruments(this.Output);
        ml.Results.Count.ShouldBe(8);
        ml.Results.Keys.Count(s => s.Contains("Desired:False-Topic:test1-Partition:0")).ShouldBe(2);
        ml.Results.Keys.Count(s => s.Contains("Desired:True-Topic:test1-Partition:1")).ShouldBe(2);
        ml.Results.Keys.Count(s => s.Contains("Desired:False-Topic:test1-Partition:2")).ShouldBe(2);
        ml.Results.Keys.Count(s => s.Contains("Desired:False-Topic:test1-Partition:3")).ShouldBe(2);

        // No assigned topic partitions
        consumer.Unassign();
        consumer.Consume(200);
        ml.RecordObservableInstruments(this.Output);
        ml.Results.Count.ShouldBe(8);
        ml.Results.Keys.Count(s => s.Contains("Desired:True-Topic:test1-Partition:1")).ShouldBe(0);
    }

    [Fact]
    public async Task ProducerTransaction()
    {
        this.Services.AddKafka(false).WithTestMockCluster(MockCluster.ClusterName);

        using MeterHelper ml = new(Statistics.EosMeterName);
        ml.RecordObservableInstruments();
        ml.Results.Count.ShouldBe(0);

        using IProducer<int, int> producer =
            this.KafkaFactory.CreateProducer<int, int>(new ProducerConfig
            {
                TransactionalId = "qwe",
                StatisticsIntervalMs = 100
            }, MockCluster.ClusterName);

        producer.Poll(TimeSpan.FromMilliseconds(500));

        ml.RecordObservableInstruments(this.Output);
        ml.Results.Count.ShouldBe(2);

        // One 1 of 4 assigned
        producer.InitTransactions(TimeSpan.FromSeconds(3));

        await Task.Delay(200);
        ml.RecordObservableInstruments(this.Output);
        ml.Results.Count.ShouldBe(2);

        producer.BeginTransaction();

        await Task.Delay(200);
        ml.RecordObservableInstruments(this.Output);
        ml.Results.Count.ShouldBe(2);

        await producer.ProduceAsync("test", new Message<int, int> { Key = 1, Value = 2 });
        ml.RecordObservableInstruments(this.Output);

        await Task.Delay(200);
        ml.RecordObservableInstruments(this.Output);
        ml.Results.Count.ShouldBe(2);

        producer.CommitTransaction();

        await Task.Delay(200);
        ml.RecordObservableInstruments(this.Output);
        ml.Results.Count.ShouldBe(2);
    }
}