// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka.PubSub.Subscription;
using Epam.Kafka.PubSub.Subscription.Options;
using Epam.Kafka.PubSub.Subscription.State;
using Epam.Kafka.PubSub.Utils;
using Shouldly;
using Xunit;

namespace Epam.Kafka.PubSub.Tests.Subscription;

public class SubscriptionOptionsExtensionsTests
{
    [Fact]
    public void IsTopicNameWithPartition_ShouldReturnFalse_WhenStateTypeIsNotGeneric()
    {
        var options = new SubscriptionOptions { StateType = typeof(InternalKafkaState) };
        var result = options.IsTopicNameWithPartition(out var storageType);
        result.ShouldBeFalse();
        storageType.ShouldBeNull();
    }

    [Fact]
    public void IsTopicNameWithPartition_ShouldReturnTrue_WhenStateTypeIsGeneric()
    {
        var options = new SubscriptionOptions { StateType = typeof(ExternalState<IExternalOffsetsStorage>) };
        var result = options.IsTopicNameWithPartition(out var storageType);
        result.ShouldBeTrue();
        storageType.ShouldBe(typeof(IExternalOffsetsStorage));
    }

    [Fact]
    public void GetTopicNames_ShouldThrowArgumentException_WhenTopicsIsNull()
    {
        var options = new SubscriptionOptions { Topics = null };
        Assert.Throws<ArgumentException>(() => options.GetTopicNames());
    }

    [Fact]
    public void GetTopicNames_ShouldReturnUniqueTopicNames()
    {
        var options = new SubscriptionOptions { Topics = "topic1; topic2; topic3" };
        var result = options.GetTopicNames();
        result.ShouldBe(new[] { "topic1", "topic2", "topic3" });
    }

    [Fact]
    public void GetTopicNames_ShouldThrowArgumentException_WhenDuplicateTopicNames()
    {
        var options = new SubscriptionOptions { Topics = "topic1; topic2; topic1" };
        Assert.Throws<ArgumentException>(() => options.GetTopicNames());
    }

    [Fact]
    public void GetTopicPartitions_ShouldThrowArgumentException_WhenTopicsIsNull()
    {
        var options = new SubscriptionOptions { Topics = null };
        Assert.Throws<ArgumentException>(() => options.GetTopicPartitions());
    }

    [Fact]
    public void GetTopicPartitions_ShouldReturnUniqueTopicPartitions()
    {
        var options = new SubscriptionOptions { Topics = "topic1    [ 0 , 1 ]   ; topic2 [0]; topic3 [1]" };
        var result = options.GetTopicPartitions();
        result.ShouldBe(new[]
        {
            new TopicPartition("topic1", 0),
            new TopicPartition("topic1", 1),
            new TopicPartition("topic2", 0),
            new TopicPartition("topic3", 1)
        });
    }

    [Fact]
    public void GetTopicPartitions_ShouldThrowArgumentException_WhenDuplicateTopicPartitions()
    {
        var options = new SubscriptionOptions { Topics = "topic1 [0,1]; topic1 [1]" };
        Assert.Throws<ArgumentException>(() => options.GetTopicPartitions());
    }

    [Fact]
    public void WithTopicPartitions_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        SubscriptionOptions options = null;
        Assert.Throws<ArgumentNullException>(() => options.WithTopicPartitions(new TopicPartition("topic1", 0)));
    }

    [Fact]
    public void WithTopicPartitions_ShouldThrowArgumentNullException_WhenTopicPartitionsIsNull()
    {
        var options = new SubscriptionOptions();
        Assert.Throws<ArgumentNullException>(() => options.WithTopicPartitions(null));
    }

    [Fact]
    public void WithTopicPartitions_ShouldThrowArgumentException_WhenTopicPartitionsIsEmpty()
    {
        var options = new SubscriptionOptions();
        Assert.Throws<ArgumentException>(() => options.WithTopicPartitions(Array.Empty<TopicPartition>()));
    }

    [Fact]
    public void WithTopicPartitions_ShouldSetTopicsProperty()
    {
        var options = new SubscriptionOptions();
        var topicPartitions = new[]
        {
            new TopicPartition("topic1", 0),
            new TopicPartition("topic2", 1)
        };

        options.WithTopicPartitions(topicPartitions);

        options.Topics.ShouldBe("topic1 [0];topic2 [1]");
    }
}