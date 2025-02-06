// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Shouldly;

using Xunit;

namespace Epam.Kafka.Tests;

public class KafkaConfigExtensionsTests
{
    [Fact]
    public void CancellationDelayMaxMs()
    {
        var cfg = new ConsumerConfig();
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
    [InlineData(DebugContext.None, "")]
    [InlineData(DebugContext.All, "all")]
    [InlineData(DebugContext.Broker, "broker")]
    [InlineData(DebugContext.Broker | DebugContext.Admin, "broker,admin")]
    public void SetDebugContext(DebugContext value, string result)
    {
        ClientConfig cfg = new ClientConfig();
        cfg.SetDebugContexts(value);
        cfg.Debug.ShouldBe(result);
    }

    [Fact]
    public void LoggerCategory()
    {
        var cfg = new ConsumerConfig();
        string defValue = cfg.GetDotnetLoggerCategory();

        Assert.Equal("Epam.Kafka.DefaultLogHandler", defValue);

        cfg.SetDotnetLoggerCategory("qwe");
        Assert.Equal("qwe", cfg.GetDotnetLoggerCategory());

        Assert.Throws<ArgumentNullException>(() => KafkaConfigExtensions.GetDotnetLoggerCategory(null!));
        Assert.Throws<ArgumentNullException>(() => KafkaConfigExtensions.SetDotnetLoggerCategory(null!, "qwe"));
        Assert.Throws<ArgumentNullException>(() => KafkaConfigExtensions.SetDotnetLoggerCategory(cfg, null!));

        cfg.Set("dotnet.logger.category", "text");
        Assert.Equal("text", cfg.GetDotnetLoggerCategory());
    }

    [Theory]
    [InlineData("qwe", "qwe")]
    [InlineData("<qwe>", "123")]
    [InlineData("a<qwe>b<QWE>c", "a123b123c")]
    public void Clone(string clientId, string expected)
    {
        var cfg = new ConsumerConfig { ClientId = clientId };

        ConsumerConfig result = cfg.Clone(new Dictionary<string, string>()
        {
            { "<qwe>", "123" }
        });

        result.ShouldNotBeNull();
        result.ShouldNotBeSameAs(cfg);

        result.ClientId.ShouldBe(expected);
    }
}