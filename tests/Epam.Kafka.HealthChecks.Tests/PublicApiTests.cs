// Copyright © 2024 EPAM Systems

using Epam.Kafka.Tests.Common;

using Xunit;

namespace Epam.Kafka.HealthChecks.Tests;

public class PublicApiTests
{
    [Fact]
    public void ApiDifferenceTests()
    {
        typeof(KafkaHealthCheckExtensions).Assembly.ShouldMatchApproved();
    }
}