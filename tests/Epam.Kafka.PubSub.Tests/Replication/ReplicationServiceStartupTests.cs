// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Publication.Options;
using Epam.Kafka.PubSub.Replication;
using Epam.Kafka.PubSub.Tests.Helpers;
using Epam.Kafka.Tests.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.Tests.Replication;

public class ReplicationServiceStartupTests : TestWithServices
{
    public ReplicationServiceStartupTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData(nameof(ReplicationOptions.DefaultTopic), "", "DefaultTopic is null or empty.")]
    [InlineData(nameof(ReplicationOptions.DefaultTopic), " ", "DefaultTopic is null or empty.")]
    [InlineData(nameof(ReplicationOptions.DefaultTopic), null, "DefaultTopic is null or empty.")]
    [InlineData(nameof(ReplicationOptions.DefaultTopic), "$%@", "DefaultTopic is not match '^[\\w|\\d|\\.|\\-]*$'.")]
    public async Task FailedOptionsValidation(string key, string? value, string expectedMessage)
    {
        using TestObserver observer = new(this, 1);

        this.ConfigurationBuilder.AddInMemoryCollection(new[]
            { new KeyValuePair<string, string?>($"Kafka:Subscriptions:{observer.Name}:Replication:{key}", value) });

        TestConversionHandler handler = new(observer);

        this.Services.AddScoped(_ => handler);

        TestDeserializer deserializer = new(observer);

        MockCluster.AddMockCluster(this)
            .AddReplication<string, TestEntityKafka, string, TestEntityKafka, TestConversionHandler>(observer.Name,
                ServiceLifetime.Scoped)
            .WithOptions(options =>
            {
                options.Topics = this.AnyTopicName;

                if (key != nameof(PublicationOptions.DefaultTopic))
                {
                    options.Replication.DefaultTopic = this.AnyTopicName;
                }
            }).WithValueDeserializer(_ => deserializer);

        OptionsValidationException exc =
            await Assert.ThrowsAsync<OptionsValidationException>(this.RunBackgroundServices);

        exc.Message.ShouldContain(expectedMessage);

        handler.Verify();
    }

    [Fact]
    public async Task DefaultValueSerializerNotAvailable()
    {
        using TestObserver observer = new(this, 1);

        MockCluster.AddMockCluster(this)
            .AddReplication<string, TestEntityKafka, string, TestEntityKafka, TestConversionHandler>(observer.Name,
                ServiceLifetime.Scoped)
            .WithOptions(options =>
            {
                options.Replication.DefaultTopic = this.AnyTopicName;
                options.Topics = this.AnyTopicName;
            });

        OptionsValidationException exc =
            await Assert.ThrowsAsync<OptionsValidationException>(this.RunBackgroundServices);

        exc.Message.ShouldContain("Custom deserializer not set for non default value type Epam.Kafka.Tests.Common.TestEntityKafka");
    }
}