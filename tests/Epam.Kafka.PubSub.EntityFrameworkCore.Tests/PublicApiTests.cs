// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.EntityFrameworkCore.Subscription.State;
using Epam.Kafka.Tests.Common;

using Xunit;

namespace Epam.Kafka.PubSub.EntityFrameworkCore.Tests;

public class PublicApiTests
{
    [Fact]
    public void ApiDifferenceTests()
    {
        typeof(KafkaTopicState).Assembly.ShouldMatchApproved();
    }
}