// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common;
using Epam.Kafka.Tests.Common;

using Xunit;

namespace Epam.Kafka.PubSub.Tests;

public class PublicApiTests
{
    [Fact]
    public void ApiDifferenceTests()
    {
        typeof(PubSubContext).Assembly.ShouldMatchApproved();
    }
}