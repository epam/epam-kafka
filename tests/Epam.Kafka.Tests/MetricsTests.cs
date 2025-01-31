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
    public void CreateDefaultClientWithMetrics()
    {
        MockCluster.AddMockCluster(this).WithClusterConfig(MockCluster.ClusterName)
            .Configure(x => x.ClientConfig.StatisticsIntervalMs = 100);

        using MeterHelper ml = new(Statistics.TopLevelMeterName);
        ml.RecordObservableInstruments();
        ml.Results.Count.ShouldBe(0);

        using IClient c1 = this.KafkaFactory.GetOrCreateClient();
        Assert.NotNull(c1);
        Task.Delay(200).Wait();
        ml.RecordObservableInstruments(this.Output);

        ml.Results.Count.ShouldBe(4);

        Task.Delay(1000).Wait();

        ml.RecordObservableInstruments(this.Output);

        ml.Results.Count.ShouldBe(4);
    }

    [Fact]
    public void ConsumerTopParMetricsAssign()
    {
        this.Services.AddKafka(false).WithTestMockCluster(MockCluster.ClusterName);

        using MeterHelper ml = new(Statistics.TopicPartitionMeterName);
        ml.RecordObservableInstruments();
        ml.Results.Count.ShouldBe(0);

        this.ServiceProvider.GetRequiredKeyedService<TestMockCluster>(MockCluster.ClusterName).SeedTopic("test1");

        using IConsumer<Ignore, Ignore> consumer =
            this.KafkaFactory.CreateConsumer<Ignore, Ignore>(new ConsumerConfig
            {
                GroupId = "qwe",
                StatisticsIntervalMs = 100
            }, MockCluster.ClusterName);

        // No assigned topic partitions
        Task.Delay(200).Wait();
        ml.RecordObservableInstruments();
        ml.Results.Count.ShouldBe(0);

        // One 1 of 4 assigned
        consumer.Assign(new TopicPartition("test1", 1));
        consumer.Consume(200);
        ml.RecordObservableInstruments(this.Output);
        ml.Results.Count.ShouldBe(1);
        ml.Results.Keys.Single().ShouldContain("Fetch:offset-query");
        ml.Results.Keys.Single().ShouldContain("Desired:true");

        // No assigned topic partitions
        consumer.Unassign();
        consumer.Consume(200);
        ml.RecordObservableInstruments();
        ml.Results.Count.ShouldBe(0);
    }
}