// Copyright © 2024 EPAM Systems


#if EF6
using Epam.Kafka.PubSub.EntityFramework6.Subscription.State;
#else
using Epam.Kafka.PubSub.EntityFrameworkCore.Subscription.State;
#endif

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