// Copyright © 2024 EPAM Systems

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
    public void ParseOk()
    {
        var value = Statistics.FromJson(
@"{
    ""name"":  ""rdkafka#producer-1"",
    ""txmsgs"": 5,
    ""rxmsgs"": 4
  }");

        value.Name.ShouldBe("rdkafka#producer-1");
        value.ClientId.ShouldBe("rdkafka");
        value.TransmittedMessagesTotal.ShouldBe(5);
        value.ConsumedMessagesTotal.ShouldBe(4);
    }
}