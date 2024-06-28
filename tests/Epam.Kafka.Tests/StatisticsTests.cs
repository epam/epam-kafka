// Copyright © 2024 EPAM Systems

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
        value.ConsumedMessagesTotal.ShouldBe(2);
        value.AgeMicroseconds.ShouldBe(40044513);

        value.Brokers.ShouldNotBeNull().Count.ShouldBe(8);
        BrokerStatistics broker = value.Brokers["sasl_ssl://kafka-4.sandbox.contoso.com:9095/534"];
        broker.Name.ShouldBe("sasl_ssl://kafka-4.sandbox.contoso.com:9095/534");
        broker.Source.ShouldBe("learned");
        broker.State.ShouldBe("UP");
        broker.StateAgeMicroseconds.ShouldBe(23884830);

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
    }
}