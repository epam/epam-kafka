// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Subscription.Options;
using Epam.Kafka.PubSub.Tests.Helpers;
using Epam.Kafka.Tests.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.Tests.Subscription;

public class SubServiceStartupTests : TestWithServices
{
    public SubServiceStartupTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData(nameof(SubscriptionOptions.BatchNotAssignedTimeout), "00:11:00", "BatchNotAssignedTimeout greater than '00:10:00'.")]
    [InlineData(nameof(SubscriptionOptions.BatchPausedTimeout), "01:00:01", "BatchPausedTimeout greater than '00:10:00'.")]
    [InlineData(nameof(SubscriptionOptions.BatchSize), "-1", "BatchSize less than '0'")]
    public async Task FailedOptionsValidation(string key, string value, string expectedMessage)
    {
        using TestObserver observer = new(this, 1);

        this.ConfigurationBuilder.AddInMemoryCollection(new[]
            { new KeyValuePair<string, string?>($"Kafka:Subscriptions:{observer.Name}:{key}", value) });

        MockCluster.AddMockCluster(this)
            .AddSubscription<string, TestEntityKafka, TestSubscriptionHandler>(observer.Name, ServiceLifetime.Scoped)
            .WithOptions(options => { options.Topics = this.AnyTopicName; });

        OptionsValidationException exc =
            await Assert.ThrowsAsync<OptionsValidationException>(this.RunBackgroundServices);

        exc.Message.ShouldContain(expectedMessage);
    }

    [Fact]
    public async Task DefaultValueSerializerNotAvailable()
    {
        using TestObserver observer = new(this, 1);

        MockCluster.AddMockCluster(this)
            .AddSubscription<string, TestEntityKafka, TestSubscriptionHandler>(observer.Name, ServiceLifetime.Scoped)
            .WithOptions(options => { options.Topics = this.AnyTopicName; });

        OptionsValidationException exc =
            await Assert.ThrowsAsync<OptionsValidationException>(this.RunBackgroundServices);

        exc.Message.ShouldContain(
            "Custom deserializer not set for non default value type");
    }

    [Fact]
    public async Task ErrorInSerializerFactory()
    {
        TestException exception = new();

        using TestObserver observer = new(this, 1);

        MockCluster.AddMockCluster(this)
            .AddSubscription<string, TestEntityKafka, TestSubscriptionHandler>(observer.Name, ServiceLifetime.Scoped)
            .WithOptions(options => { options.Topics = this.AnyTopicName; })
            .WithKeyDeserializer(_ => throw exception).WithValueDeserializer(_ => throw exception);

        TestException exc =
            await Assert.ThrowsAsync<TestException>(this.RunBackgroundServices);
    }
}