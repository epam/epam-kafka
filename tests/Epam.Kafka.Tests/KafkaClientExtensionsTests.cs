// Copyright © 2024 EPAM Systems

using Xunit;

namespace Epam.Kafka.Tests;

public class KafkaClientExtensionsTests
{
    [Fact]
    public void ArgumentExceptions()
    {
        Assert.Throws<ArgumentNullException>(() => KafkaClientExtensions.CreateDependentAdminClient(null!));
        Assert.Throws<ArgumentNullException>(() => KafkaClientExtensions.CreateDependentProducer<int, int>(null!));
    }
}