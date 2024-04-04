// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Publication.Options;
using Epam.Kafka.PubSub.Tests.Helpers;
using Epam.Kafka.Tests.Common;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.Tests.Publication;

public class PubServiceStartupTests : TestWithServices
{
    public PubServiceStartupTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData(nameof(PublicationOptions.HandlerTimeout), "00:00:01", "HandlerTimeout less than '00:01:00'")]
    [InlineData(nameof(PublicationOptions.HandlerTimeout), "01:00:01", "HandlerTimeout greater than '01:00:00'")]
    [InlineData(nameof(PublicationOptions.DefaultTopic), "", "DefaultTopic is null or empty.")]
    [InlineData(nameof(PublicationOptions.DefaultTopic), " ", "DefaultTopic is null or empty.")]
    [InlineData(nameof(PublicationOptions.DefaultTopic), null, "DefaultTopic is null or empty.")]
    [InlineData(nameof(PublicationOptions.DefaultTopic), "$%@", "DefaultTopic is not match '^[\\w|\\d|\\.|\\-]*$'.")]
    public async Task FailedOptionsValidation(string key, string? value, string expectedMessage)
    {
        using TestObserver observer = new(this, 1);

        this.ConfigurationBuilder.AddInMemoryCollection(new[]
            { new KeyValuePair<string, string?>($"Kafka:Publications:{observer.Name}:{key}", value) });

        TestPublicationHandler handler = new(false, observer);

        this.Services.AddScoped(_ => handler);

        MockCluster.AddMockCluster(this)
            .AddPublication<string, TestEntityKafka, TestPublicationHandler>(observer.Name, ServiceLifetime.Scoped)
            .WithOptions(options =>
            {
                if (key != nameof(PublicationOptions.DefaultTopic))
                {
                    options.DefaultTopic = this.AnyTopicName;
                }
            });

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
            .AddPublication<string, TestEntityKafka, TestPublicationHandler>(observer.Name, ServiceLifetime.Scoped)
            .WithOptions(options => { options.DefaultTopic = this.AnyTopicName; });

        OptionsValidationException exc =
            await Assert.ThrowsAsync<OptionsValidationException>(this.RunBackgroundServices);

        exc.Message.ShouldContain("Custom serializer not set for non default value type");
    }

    [Fact]
    public async Task FailedHandlerCreationDueToServiceCollectionSetup()
    {
        using TestObserver observer = new(this, 2);

        TestSerializer serializer = new(observer);

        MockCluster.AddMockCluster(this)
            .AddPublication<string, TestEntityKafka, TestPublicationHandler>(observer.Name, ServiceLifetime.Scoped)
            .WithValueSerializer(_ => serializer)
            .WithOptions(options => { options.DefaultTopic = this.AnyTopicName; });

        InvalidOperationException exc = await Assert.ThrowsAsync<InvalidOperationException>(this.RunBackgroundServices);

        exc.Message.ShouldContain("Unable to resolve service for type 'System.Boolean' while attempting to activate");

        observer.AssertStart();
        observer.AssertStop(exc);
    }

    [Fact]
    public async Task ErrorInSerializerFactory()
    {
        TestException exception = new ();

        using TestObserver observer = new(this, 1);

        MockCluster.AddMockCluster(this)
            .AddPublication<string, TestEntityKafka, TestPublicationHandler>(observer.Name, ServiceLifetime.Scoped)
            .WithKeySerializer(_ => throw exception).WithValueSerializer(_ => throw exception)
            .WithOptions(options => { options.DefaultTopic = this.AnyTopicName; });

        TestException exc =
            await Assert.ThrowsAsync<TestException>(this.RunBackgroundServices);
    }
}