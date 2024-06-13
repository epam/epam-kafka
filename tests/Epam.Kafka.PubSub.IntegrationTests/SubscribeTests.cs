// Copyright © 2024 EPAM Systems

using Epam.Kafka.Tests.Common;
using Xunit;

namespace Epam.Kafka.PubSub.IntegrationTests;

// This class has no code, and is never created. Its purpose is simply
// to be the place to apply [CollectionDefinition] and all the
// ICollectionFixture<> interfaces.

[CollectionDefinition(Name, DisableParallelization = false)]
public class SubscribeTests : ICollectionFixture<MockCluster>
{
    public const string Name = "Subscribe";
}