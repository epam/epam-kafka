// Copyright © 2024 EPAM Systems

using System.Text.Json;

using Confluent.Kafka;

using Epam.Kafka.Metrics;
using Epam.Kafka.Stats;
using Epam.Kafka.Stats.Broker;
using Epam.Kafka.Stats.Eos;
using Epam.Kafka.Stats.Group;
using Epam.Kafka.Stats.Topic;
using Epam.Kafka.Tests.Common;

using Shouldly;

using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.Tests;

public class StatisticsTests
{
    public ITestOutputHelper Output { get; }

    public StatisticsTests(ITestOutputHelper output)
    {
        this.Output = output ?? throw new ArgumentNullException(nameof(output));
    }

    [Fact]
    public void ParseErrors()
    {
        Assert.Throws<ArgumentNullException>(() => Statistics.FromJson(null!));
        Assert.Throws<ArgumentException>(() => Statistics.FromJson(""));
        Assert.Throws<ArgumentException>(() => Statistics.FromJson("not a json"));
    }

    [Fact]
    public void SerializeOk()
    {
        Statistics statistics = new Statistics();
        statistics.Brokers.Add("b1", new BrokerStatistics());
        TopicStatistics t = new TopicStatistics();
        t.Partitions.Add(0, new PartitionStatistics());
        statistics.Topics.Add("t1", t);

        string value = JsonSerializer.Serialize(statistics, StatsJsonContext.Default.Statistics);

        this.Output.WriteLine(value);
    }

    [Fact]
    public void ParseOk()
    {
        using Stream json = typeof(StatisticsTests).Assembly.GetManifestResourceStream("Epam.Kafka.Tests.Data.ConsumerStat.json")!;
        using var reader = new StreamReader(json);

        var value = Statistics.FromJson(reader.ReadToEnd());

        value.Name.ShouldBe("Epam.Kafka.Sample@QWE:Sample#consumer-2");
        value.ClientId.ShouldBe("Epam.Kafka.Sample@QWE:Sample");
        value.Type.ShouldBe("consumer");
        value.ClockMicroseconds.ShouldBe(185203076223);
        value.TimeEpochSeconds.ShouldBe(1719564501);
        value.AgeMicroseconds.ShouldBe(40044513);
        value.OpsQueueCountGauge.ShouldBe(555);
        value.ProducerQueueCountGauge.ShouldBe(78);
        value.ProducerQueueSizeGauge.ShouldBe(79);
        value.ProducerQueueMax.ShouldBe(80);
        value.ProducerQueueSizeMax.ShouldBe(81);
        value.TransmittedMessagesTotal.ShouldBe(323);
        value.ConsumedMessagesTotal.ShouldBe(2);

        value.Brokers.ShouldNotBeNull().Count.ShouldBe(8);
        BrokerStatistics broker = value.Brokers["sasl_ssl://kafka-4.sandbox.contoso.com:9095/534"];
        broker.Name.ShouldBe("sasl_ssl://kafka-4.sandbox.contoso.com:9095/534");
        broker.NodeId.ShouldBe(534);
        broker.NodeName.ShouldBe("kafka-4.sandbox.contoso.com:9095");
        broker.Source.ShouldBe(BrokerSource.Learned);
        broker.State.ShouldBe(BrokerState.ApiVersionQuery);
        broker.StateAgeMicroseconds.ShouldBe(23884830);

        broker.TopicPartitions.ShouldNotBeNull().Count.ShouldBe(1);
        TopicPartition tp = broker.TopicPartitions["epam-kafka-sample-topic-2-0"];
        tp.Topic.ShouldBe("epam-kafka-sample-topic-2");
        tp.Partition.ShouldBe((Partition)0);

        value.Topics.ShouldNotBeNull().Count.ShouldBe(1);
        TopicStatistics topic = value.Topics["epam-kafka-sample-topic-2"];
        topic.Name.ShouldBe("epam-kafka-sample-topic-2");
        topic.AgeMilliseconds.ShouldBe(23753);
        topic.MetadataAgeMilliseconds.ShouldBe(35918);
        topic.BatchSize.Sum.ShouldBe(18808985);
        topic.BatchCount.Sum.ShouldBe(480028);

        topic.Partitions.ShouldNotBeNull().Count.ShouldBe(2);
        PartitionStatistics partition = topic.Partitions[0];
        partition.Id.ShouldBe(0);
        partition.CommittedOffset.ShouldBe(11);
        partition.HiOffset.ShouldBe(12);
        partition.LsOffset.ShouldBe(12);
        partition.LoOffset.ShouldBe(10);
        partition.ConsumerLag.ShouldBe(1);
        partition.FetchState.ShouldBe(PartitionFetchState.OffsetQuery);

        GroupStatistics group = value.ConsumerGroup.ShouldNotBeNull();
        group.State.ShouldBe(GroupState.Up);
        group.StateAgeMilliseconds.ShouldBe(39225);
        group.JoinState.ShouldBe(GroupJoinState.WaitJoin);
        group.RebalanceAgeMilliseconds.ShouldBe(35748);
        group.RebalanceCount.ShouldBe(1);

        EosStatistics transaction = value.ExactlyOnceSemantics.ShouldNotBeNull();
        transaction.TransactionState.ShouldBe(TransactionalProducerState.InTransaction);
        transaction.IdempotentState.ShouldBe(IdempotentProducerIdState.None);
    }

    [Fact]
    public void TopLevelMetricsTests()
    {
        using MeterHelper ml = new(Statistics.TopLevelMeterName);

        ConsumerMetrics cm = new(new ConsumerConfig());
        ProducerMetrics pm = new();

        cm.OnNext(new Statistics { ClientId = "c1", Name = "n1", Type = "c", ConsumedMessagesTotal = 123, OpsQueueCountGauge = 332 });
        pm.OnNext(new Statistics { ClientId = "c1", Name = "n2", Type = "p", TransmittedMessagesTotal = 111 });

        ml.RecordObservableInstruments(this.Output);

        ml.Results.Count.ShouldBe(8);
        ml.Results["epam_kafka_stats_trx_msgs_Handler:n1-Name:c1-Type:c"].ShouldBe(123);
        ml.Results["epam_kafka_stats_trx_msgs_Handler:n2-Name:c1-Type:p"].ShouldBe(111);
        ml.Results["epam_kafka_stats_age_Handler:n1-Name:c1-Type:c"].ShouldBe(0);
        ml.Results["epam_kafka_stats_age_Handler:n2-Name:c1-Type:p"].ShouldBe(0);

        cm.OnCompleted();

        cm.OnNext(new Statistics { ClientId = "c1", Name = "n1", ConsumedMessagesTotal = 124 });
        pm.OnNext(new Statistics { ClientId = "p1", Name = "n1", TransmittedMessagesTotal = 112, AgeMicroseconds = 555000000 });

        ml.RecordObservableInstruments(this.Output);

        ml.Results.Count.ShouldBe(4);
        ml.Results["epam_kafka_stats_trx_msgs_Handler:n2-Name:c1-Type:p"].ShouldBe(112);
        ml.Results["epam_kafka_stats_age_Handler:n2-Name:c1-Type:p"].ShouldBe(555);

        pm.OnCompleted();

        ml.RecordObservableInstruments(this.Output);
        ml.Results.Count.ShouldBe(0);
    }

    [Fact]
    public void TopParMetricsTests()
    {
        using MeterHelper ml = new(Statistics.TopicPartitionMeterName);

        ConsumerMetrics cm = new(new ConsumerConfig { GroupId = "qwe" });

        Statistics statistics = new Statistics { ClientId = "c1", Name = "n1", Type = "c", ConsumedMessagesTotal = 123 };
        TopicStatistics ts = new TopicStatistics { Name = "t1" };
        PartitionStatistics ps = new PartitionStatistics { Id = 2, ConsumerLag = 445, Desired = true, FetchState = PartitionFetchState.Active };

        ts.Partitions.Add(ps.Id, ps);

        statistics.Topics.Add(ts.Name, ts);

        cm.OnNext(statistics);

        ml.RecordObservableInstruments(this.Output);

        ml.Results.Count.ShouldBe(2);
        ml.Results["epam_kafka_stats_tp_lag_Group:qwe-Handler:n1-Name:c1-Type:c-Desired:True-Topic:t1-Partition:2"].ShouldBe(445);
        ml.Results["epam_kafka_stats_tp_fetch_state_Group:qwe-Handler:n1-Name:c1-Type:c-Desired:True-Topic:t1-Partition:2"].ShouldBe(5);

        statistics.Topics.Clear();

        cm.OnNext(statistics);

        ml.RecordObservableInstruments(this.Output);
        ml.Results.Count.ShouldBe(0);

        cm.OnCompleted();
    }

    [Fact]
    public void TransactionMetricsTests()
    {
        using MeterHelper ml = new(Statistics.EosMeterName);

        ProducerMetrics cm = new();

        Statistics statistics = new Statistics { ClientId = "c1", Name = "n1", Type = "c" };
        statistics.ExactlyOnceSemantics.TransactionState = TransactionalProducerState.Ready;
        statistics.ExactlyOnceSemantics.IdempotentState = IdempotentProducerIdState.Assigned;

        cm.OnNext(statistics);
        ml.RecordObservableInstruments(this.Output);
        ml.Results.Count.ShouldBe(2);
        ml.Results["epam_kafka_stats_eos_txn_state_Handler:n1-Name:c1-Type:c"].ShouldBe(4);
        ml.Results["epam_kafka_stats_eos_idemp_state_Handler:n1-Name:c1-Type:c"].ShouldBe(7);

        statistics.ExactlyOnceSemantics.TransactionState = TransactionalProducerState.InTransaction;
        cm.OnNext(statistics);
        ml.RecordObservableInstruments(this.Output);
        ml.Results.Count.ShouldBe(2);
        ml.Results["epam_kafka_stats_eos_txn_state_Handler:n1-Name:c1-Type:c"].ShouldBe(5);
        ml.Results["epam_kafka_stats_eos_idemp_state_Handler:n1-Name:c1-Type:c"].ShouldBe(7);

        cm.OnCompleted();
    }

    [Fact]
    public void MetricsAfterErrorTests()
    {
        using MeterHelper ml = new(Statistics.EosMeterName);

        ProducerMetrics cm = new();

        Statistics statistics = new Statistics { ClientId = "c1", Name = "n1", Type = "c" };
        statistics.ExactlyOnceSemantics.TransactionState = TransactionalProducerState.Ready;
        statistics.ExactlyOnceSemantics.IdempotentState = IdempotentProducerIdState.Assigned;

        cm.OnNext(statistics);
        ml.RecordObservableInstruments(this.Output);
        ml.Results.Count.ShouldBe(2);
        ml.Results["epam_kafka_stats_eos_txn_state_Handler:n1-Name:c1-Type:c"].ShouldBe(4);
        ml.Results["epam_kafka_stats_eos_idemp_state_Handler:n1-Name:c1-Type:c"].ShouldBe(7);

        cm.OnError(new InvalidOperationException("test"));
        ml.RecordObservableInstruments();
        ml.Results.Count.ShouldBe(0);

        cm.OnNext(statistics);
        ml.RecordObservableInstruments(this.Output);
        ml.Results.Count.ShouldBe(2);
        ml.Results["epam_kafka_stats_eos_txn_state_Handler:n1-Name:c1-Type:c"].ShouldBe(4);
        ml.Results["epam_kafka_stats_eos_idemp_state_Handler:n1-Name:c1-Type:c"].ShouldBe(7);

        cm.OnCompleted();
    }

    [Fact]
    public void ConsumerGroupMetricsTests()
    {
        using MeterHelper ml = new(Statistics.ConsumerGroupMeterName);

        ConsumerMetrics cm = new(new ConsumerConfig { GroupId = "qwe" });

        Statistics statistics = new Statistics { ClientId = "c1", Name = "n1", Type = "c" };
        statistics.ConsumerGroup.State = GroupState.Up;
        statistics.ConsumerGroup.JoinState = GroupJoinState.WaitAssign;
        statistics.ConsumerGroup.RebalanceCount = 22;
        statistics.ConsumerGroup.RebalanceAgeMilliseconds = 12000;
        statistics.ConsumerGroup.AssignmentCount = 4;

        cm.OnNext(statistics);

        ml.RecordObservableInstruments(this.Output);

        ml.Results.Count.ShouldBe(5);
        ml.Results["epam_kafka_stats_cg_state_Handler:n1-Name:c1-Type:c"].ShouldBe(4);
        ml.Results["epam_kafka_stats_cg_join_state_Handler:n1-Name:c1-Type:c"].ShouldBe(7);
        ml.Results["epam_kafka_stats_cg_rebalance_age_Handler:n1-Name:c1-Type:c"].ShouldBe(12);
        ml.Results["epam_kafka_stats_cg_rebalance_count_Handler:n1-Name:c1-Type:c"].ShouldBe(22);
        ml.Results["epam_kafka_stats_cg_assignment_count_Handler:n1-Name:c1-Type:c"].ShouldBe(4);

        cm.OnCompleted();
    }
}