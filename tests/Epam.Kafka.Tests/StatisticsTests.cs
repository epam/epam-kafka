// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.Metrics;
using Epam.Kafka.Stats;
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
    public void ParseConsumerOk()
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
        broker.Source.ShouldBe("learned");
        broker.State.ShouldBe("UP");
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
        partition.FetchState.ShouldBe("active");

        GroupStatistics group = value.ConsumerGroup.ShouldNotBeNull();
        group.State.ShouldBe("up");
        group.StateAgeMilliseconds.ShouldBe(39225);
        group.JoinState.ShouldBe("steady");
        group.RebalanceAgeMilliseconds.ShouldBe(35748);
        group.RebalanceCount.ShouldBe(1);
    }

    [Fact]
    public void TopLevelMetricsTests()
    {
        using MeterHelper ml = new(Statistics.TopLevelMeterName);

        ConsumerMetrics cm = new(new ConsumerConfig());
        ProducerMetrics pm = new(new ProducerConfig());

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

        ml.Results.Clear();
        ml.RecordObservableInstruments(this.Output);

        ml.Results.Count.ShouldBe(4);
        ml.Results["epam_kafka_stats_trx_msgs_Handler:n2-Name:c1-Type:p"].ShouldBe(112);
        ml.Results["epam_kafka_stats_age_Handler:n2-Name:c1-Type:p"].ShouldBe(555);

        pm.OnCompleted();

        ml.Results.Clear();
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
        PartitionStatistics ps = new PartitionStatistics { Id = 2, ConsumerLag = 445, Desired = true, FetchState = "active" };

        ts.Partitions.Add(ps.Id, ps);

        statistics.Topics.Add(ts.Name, ts);

        cm.OnNext(statistics);

        ml.RecordObservableInstruments(this.Output);

        ml.Results.Count.ShouldBe(1);
        ml.Results["epam_kafka_stats_tp_lag_Handler:n1-Name:c1-Type:c-Desired:True-Fetch:active-Topic:t1-Partition:2-Group:qwe"].ShouldBe(445);

        statistics.Topics.Clear();

        cm.OnNext(statistics);

        ml.Results.Clear();
        ml.RecordObservableInstruments(this.Output);
        ml.Results.Count.ShouldBe(0);

        cm.OnCompleted();
    }

    [Fact]
    public void TransactionMetricsTests()
    {
        using MeterHelper ml = new(Statistics.TransactionMeterName);

        ProducerMetrics cm = new(new ProducerConfig { TransactionalId = "qwe" });

        Statistics statistics = new Statistics { ClientId = "c1", Name = "n1", Type = "c", ConsumedMessagesTotal = 123 };
        statistics.ProducerTransaction.EnqAllowed = true;
        statistics.ProducerTransaction.TransactionState = "test";
        statistics.ProducerTransaction.TransactionAgeMilliseconds = 120000;

        cm.OnNext(statistics);

        ml.RecordObservableInstruments(this.Output);

        ml.Results.Count.ShouldBe(1);
        ml.Results["epam_kafka_stats_eos_txn_age_Handler:n1-Name:c1-Type:c-Enqueue:True-State:test-Transaction:qwe"].ShouldBe(120);

        cm.OnCompleted();
    }
}