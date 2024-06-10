// Copyright © 2024 EPAM Systems

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;

namespace Epam.Kafka.Tests;

public class KafkaBuilderTests
{
    [Fact]
    public void ArgumentExceptions()
    {
        KafkaBuilder kafkaBuilder = new KafkaBuilder(new ServiceCollection(), false);

        Assert.Throws<ArgumentNullException>(() => kafkaBuilder.WithClusterConfig(null!));
        Assert.Throws<ArgumentNullException>(() => kafkaBuilder.WithProducerConfig(null!));
        Assert.Throws<ArgumentNullException>(() => kafkaBuilder.WithConsumerConfig(null!));
        Assert.Throws<ArgumentNullException>(() => kafkaBuilder.WithDefaults(null!));
        Assert.Throws<ArgumentNullException>(() => kafkaBuilder.WithConfigPlaceholders(null!));
    }

    [Theory]
    [InlineData("<qwe", "any", "Placeholder key")]
    [InlineData("qwe>", "any", "Placeholder key")]
    [InlineData("<qwe>", "<any>", "Placeholder value")]
    [InlineData("<qwe>", null, "Placeholder value")]
    [InlineData("<d>", "any", "Duplicate")]
    [InlineData("<D>", "any", "Duplicate")]
    public void PlaceholderArgumentExceptions(string key, string? value, string msg)
    {
        KafkaBuilder kafkaBuilder = new KafkaBuilder(new ServiceCollection(), true);

        Assert.Throws<ArgumentException>(() => kafkaBuilder.WithConfigPlaceholders(new KeyValuePair<string, string>[]
        {
             new("<d>", "v") ,
             new(key, value! )
        })).Message.ShouldContain(msg);
    }
}