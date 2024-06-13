﻿// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Shouldly;
using Xunit;

namespace Epam.Kafka.Tests;

public class KafkaConfigExtensionsTests
{
    [Fact]
    public void CancellationDelayMaxMs()
    {
        ConsumerConfig cfg = new ConsumerConfig();
        int defValue = cfg.GetCancellationDelayMaxMs();

        Assert.Equal(100, defValue);

        cfg.SetCancellationDelayMaxMs(5000);
        Assert.Equal(5000, cfg.GetCancellationDelayMaxMs());

        Assert.Throws<ArgumentNullException>(() => KafkaConfigExtensions.GetCancellationDelayMaxMs(null!));
        Assert.Throws<ArgumentNullException>(() => KafkaConfigExtensions.SetCancellationDelayMaxMs(null!, 5));

        Assert.Throws<ArgumentOutOfRangeException>(() => cfg.SetCancellationDelayMaxMs(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => cfg.SetCancellationDelayMaxMs(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => cfg.SetCancellationDelayMaxMs(10001));

        cfg.Set("dotnet.cancellation.delay.max.ms", "text");
        Assert.Throws<ArgumentException>(() => cfg.GetCancellationDelayMaxMs());

        cfg.Set("dotnet.cancellation.delay.max.ms", "0");
        Assert.Throws<ArgumentOutOfRangeException>(() => cfg.GetCancellationDelayMaxMs());

        cfg.Set("dotnet.cancellation.delay.max.ms", "10001");
        Assert.Throws<ArgumentOutOfRangeException>(() => cfg.GetCancellationDelayMaxMs());

        cfg.Set("dotnet.cancellation.delay.max.ms", "-1");
        Assert.Throws<ArgumentOutOfRangeException>(() => cfg.GetCancellationDelayMaxMs());
    }

    [Theory]
    [InlineData("qwe","qwe")]
    [InlineData("<qwe>","123")]
    [InlineData("a<qwe>b<QWE>c","a123b123c")]
    public void Clone(string clientId, string expected)
    {
        ConsumerConfig cfg = new ConsumerConfig { ClientId = clientId };

        ConsumerConfig result = cfg.Clone(new Dictionary<string, string>()
        {
            { "<qwe>", "123" }
        });

        result.ShouldNotBeNull();
        result.ShouldNotBeSameAs(cfg);

        result.ClientId.ShouldBe(expected);
    }
}