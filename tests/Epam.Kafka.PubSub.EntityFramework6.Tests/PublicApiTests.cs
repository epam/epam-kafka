// Copyright © 2024 EPAM Systems


using Epam.Kafka.PubSub.EntityFramework6.Subscription.State;

using Epam.Kafka.Tests.Common;

using Xunit;

namespace Epam.Kafka.PubSub.EntityFramework6.Tests;

public class PublicApiTests
{
    [Fact]
    public void ApiDifferenceTests()
    {
        typeof(KafkaTopicState).Assembly.ShouldMatchApproved();
    }
}