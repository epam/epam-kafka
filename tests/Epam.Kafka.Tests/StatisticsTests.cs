// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

using Confluent.Kafka;
using Epam.Kafka.Metrics;
using Epam.Kafka.Stats;

using Shouldly;

using Xunit;

namespace Epam.Kafka.Tests;

public class StatisticsTests
{
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

        topic.Partitions.ShouldNotBeNull().Count.ShouldBe(2);
        PartitionStatistics partition = topic.Partitions[0];
        partition.Id.ShouldBe(0);
        partition.CommittedOffset.ShouldBe(11);
        partition.HiOffset.ShouldBe(12);
        partition.LsOffset.ShouldBe(12);
        partition.LoOffset.ShouldBe(10);
        partition.ConsumerLag.ShouldBe(1);
        partition.FetchState.ShouldBe("active");

        GroupStatistics group = value.ConsumerGroups.ShouldNotBeNull();
        group.State.ShouldBe("up");
        group.StateAgeMilliseconds.ShouldBe(39225);
        group.JoinState.ShouldBe("steady");
        group.RebalanceAgeMilliseconds.ShouldBe(35748);
        group.RebalanceCount.ShouldBe(1);
    }

    [Fact]
    public void TopLevelMetricsTests()
    {
        MeterListener ml = new MeterListener();

        ml.InstrumentPublished = (instrument, listener) => { listener.EnableMeasurementEvents(instrument); };

        Dictionary<string, long> results = new();

        ml.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
        {
            string ts = string.Join("-", tags.ToArray().Select(x => $"{x.Key}:{x.Value}"));

            string key = $"{instrument.Name}_{ts}";

            results[key] = measurement;
        });

        ml.Start();

        ConsumerMetrics cm = new();
        ProducerMetrics pm = new();

        cm.OnNext(new Statistics { ClientId = "c1", Name = "n1", Type = "c", ConsumedMessagesTotal = 123 });
        pm.OnNext(new Statistics { ClientId = "c1", Name = "n2", Type = "p", TransmittedMessagesTotal = 111 });

        ml.RecordObservableInstruments();
        results.Count.ShouldBe(4);
        results["epam_kafka_stats_rxmsgs_Name:c1-Handler:n1-Instance:c"].ShouldBe(123);
        results["epam_kafka_stats_txmsgs_Name:c1-Handler:n2-Instance:p"].ShouldBe(111);
        results["epam_kafka_stats_age_Name:c1-Handler:n1-Instance:c"].ShouldBe(0);
        results["epam_kafka_stats_age_Name:c1-Handler:n2-Instance:p"].ShouldBe(0);

        cm.OnCompleted();

        cm.OnNext(new Statistics { ClientId = "c1", Name = "n1", ConsumedMessagesTotal = 124 });
        pm.OnNext(new Statistics { ClientId = "p1", Name = "n1", TransmittedMessagesTotal = 112, AgeMicroseconds = 555});

        results.Clear();
        ml.RecordObservableInstruments();

        results.Count.ShouldBe(2);
        results["epam_kafka_stats_txmsgs_Name:c1-Handler:n2-Instance:p"].ShouldBe(112);
        results["epam_kafka_stats_age_Name:c1-Handler:n2-Instance:p"].ShouldBe(555);

        pm.OnCompleted();

        results.Clear();
        ml.RecordObservableInstruments();
        results.Count.ShouldBe(0);
    }
}