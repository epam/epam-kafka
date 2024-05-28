// Copyright © 2024 EPAM Systems

using Confluent.Kafka;


using Epam.Kafka.PubSub.Subscription;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using Xunit;
using Xunit.Abstractions;

#if EF6
using Epam.Kafka.PubSub.EntityFramework6.Subscription.State;
using Epam.Kafka.PubSub.EntityFramework6.Tests.Helpers;

namespace Epam.Kafka.PubSub.EntityFramework6.Tests;
#else
using Epam.Kafka.PubSub.EntityFrameworkCore.Subscription.State;
using Epam.Kafka.PubSub.EntityFrameworkCore.Tests.Helpers;

namespace Epam.Kafka.PubSub.EntityFrameworkCore.Tests;
#endif



public class DbContextOffsetsStorageTests : TestWithContext
{
    public DbContextOffsetsStorageTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void SingleTopic()
    {
        const string topic = "anyTopic";
        const string consumerGroup = "anyGroup";

        this.Services.TryAddKafkaDbContextState<TestContext>();

        using IServiceScope scope = this.ServiceProvider.CreateScope();

        IExternalOffsetsStorage provider = scope.ServiceProvider.GetRequiredService<IExternalOffsetsStorage>();

        IReadOnlyCollection<TopicPartitionOffset> items = provider.GetOrCreate(new[] { new TopicPartition(topic, 0) },
            consumerGroup, CancellationToken.None);
        items.ShouldHaveSingleItem().Offset.ShouldBe(Offset.Unset);

        IReadOnlyCollection<TopicPartitionOffset> result =
            provider.CommitOrReset(new[] { new TopicPartitionOffset(topic, 0, 100) }, consumerGroup,
                CancellationToken.None);
        result.ShouldHaveSingleItem().Offset.ShouldBe(100);
    }

    [Theory]
    [InlineData(22, true, -1)]
    [InlineData(22, false, 22)]
    [InlineData(-1, true, -1)]
    [InlineData(-1, false, -1)]
    public void GetPausedTopic(int dbOffset, bool paused, int expectedOffset)
    {
        const string topic = "anyTopic";
        const string consumerGroup = "anyGroup";

        this.Services.TryAddKafkaDbContextState<TestContext>();

        using IServiceScope scope = this.ServiceProvider.CreateScope();

        this.SeedData(new KafkaTopicState
        { Topic = topic, ConsumerGroup = consumerGroup, Offset = dbOffset, Pause = paused });

        IExternalOffsetsStorage provider = scope.ServiceProvider.GetRequiredService<IExternalOffsetsStorage>();

        IReadOnlyCollection<TopicPartitionOffset> items = provider.GetOrCreate(new[] { new TopicPartition(topic, 0) },
            consumerGroup, CancellationToken.None);
        items.ShouldHaveSingleItem().Offset.ShouldBe(expectedOffset);
    }

    [Theory]
    [InlineData(25, false, 25, 25)]
    [InlineData(25, true, -1, 25)]
    [InlineData(-1, true, -1, 30)]
    public void SetPausedTopic(int reportedOffset, bool paused, int expectedOffsetInResult,
        int expectedOffsetInDatabase)
    {
        const string topic = "anyTopic";
        const string consumerGroup = "anyGroup";

        this.Services.TryAddKafkaDbContextState<TestContext>();

        using IServiceScope scope1 = this.ServiceProvider.CreateScope();

        TestContext context = scope1.ServiceProvider.GetRequiredService<TestContext>();

        // initial offset
        context.KafkaTopicStates.Add(new KafkaTopicState
        { Topic = topic, Offset = 30, ConsumerGroup = consumerGroup, Pause = paused });
        context.SaveChanges();

        IExternalOffsetsStorage provider = scope1.ServiceProvider.GetRequiredService<IExternalOffsetsStorage>();

        provider.GetOrCreate(new[] { new TopicPartition(topic, 0) }, consumerGroup, CancellationToken.None);

        IReadOnlyCollection<TopicPartitionOffset> result =
            provider.CommitOrReset(new[] { new TopicPartitionOffset(topic, 0, reportedOffset) }, consumerGroup,
                CancellationToken.None);
        result.ShouldHaveSingleItem().Offset.ShouldBe(expectedOffsetInResult);

        using IServiceScope scope2 = this.ServiceProvider.CreateScope();

        TestContext context2 = scope1.ServiceProvider.GetRequiredService<TestContext>();
        context2.KafkaTopicStates.ShouldHaveSingleItem().Offset.ShouldBe(expectedOffsetInDatabase);
        context2.KafkaTopicStates.ShouldHaveSingleItem().Pause.ShouldBe(paused);
    }

    [Fact]
    public void SingleTopicConcurrency()
    {
        const string topic = "anyTopic";
        const string consumerGroup = "anyGroup";

        this.Services.TryAddKafkaDbContextState<TestContext>();

        using IServiceScope scope1 = this.ServiceProvider.CreateScope();

        using IServiceScope scope2 = this.ServiceProvider.CreateScope();

        TestContext context = scope2.ServiceProvider.GetRequiredService<TestContext>();

        // initial offset
        context.KafkaTopicStates.Add(new KafkaTopicState { Topic = topic, Offset = 30, ConsumerGroup = consumerGroup });
        context.SaveChanges();

        IExternalOffsetsStorage provider = scope1.ServiceProvider.GetRequiredService<IExternalOffsetsStorage>();

        IReadOnlyCollection<TopicPartitionOffset> items = provider.GetOrCreate(new[] { new TopicPartition(topic, 0) },
            consumerGroup, CancellationToken.None);
        items.ShouldHaveSingleItem().Offset.ShouldBe(30);

        // offset changed by another thread/app
        context.KafkaTopicStates.Single().Offset = 45;
        context.SaveChanges();

        // take external offset instead of committed one
        IReadOnlyCollection<TopicPartitionOffset> result =
            provider.CommitOrReset(new[] { new TopicPartitionOffset(topic, 0, 100) }, consumerGroup,
                CancellationToken.None);
        result.ShouldHaveSingleItem().Offset.ShouldBe(45);
    }
}