// Copyright © 2024 EPAM Systems

using Microsoft.Extensions.DependencyInjection;
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
    }
}