// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Utils;

using Shouldly;

using Xunit;

namespace Epam.Kafka.PubSub.Tests.Utils;

public class OffsetsExtensionsTests
{
    [Fact]
    public void GetOffsetsRange_ShouldThrowArgumentNullException_WhenResultsIsNull()
    {
        IEnumerable<ConsumeResult<string, string>>? results = null;
        Assert.Throws<ArgumentNullException>(() => results.GetOffsetsRange(out IDictionary<TopicPartition, Offset>? from, out IDictionary<TopicPartition, Offset>? to));
    }

    [Fact]
    public void GetOffsetsRange_ShouldReturnCorrectOffsetsRange()
    {
        var results = new List<ConsumeResult<string, string>>
            {
                new() {
                    Topic = "topic1",
                    Partition = new Partition(0),
                    Offset = new Offset(5)
                },
                new() {
                    Topic = "topic1",
                    Partition = new Partition(0),
                    Offset = new Offset(10)
                },
                new() {
                    Topic = "topic1",
                    Partition = new Partition(1),
                    Offset = new Offset(3)
                },
                new() {
                    Topic = "topic1",
                    Partition = new Partition(1),
                    Offset = new Offset(8)
                }
            };

        results.GetOffsetsRange(out IDictionary<TopicPartition, Offset>? from, out IDictionary<TopicPartition, Offset>? to);

        from.ShouldContainKeyAndValue(new TopicPartition("topic1", new Partition(0)), new Offset(5));
        from.ShouldContainKeyAndValue(new TopicPartition("topic1", new Partition(1)), new Offset(3));
        to.ShouldContainKeyAndValue(new TopicPartition("topic1", new Partition(0)), new Offset(10));
        to.ShouldContainKeyAndValue(new TopicPartition("topic1", new Partition(1)), new Offset(8));
    }

    [Fact]
    public void PrepareOffsetsToCommit_ShouldThrowArgumentNullException_WhenItemsIsNull()
    {
        IDictionary<TopicPartition, Offset>? items = null;
        Assert.Throws<ArgumentNullException>(items.PrepareOffsetsToCommit);
    }

    [Fact]
    public void PrepareOffsetsToCommit_ShouldReturnCorrectOffsets()
    {
        var items = new Dictionary<TopicPartition, Offset>
            {
                { new TopicPartition("topic1", new Partition(0)), new Offset(5) },
                { new TopicPartition("topic1", new Partition(1)), new Offset(10) }
            };

        IReadOnlyCollection<TopicPartitionOffset> result = items.PrepareOffsetsToCommit();

        result.ShouldContain(new TopicPartitionOffset(new TopicPartition("topic1", new Partition(0)), new Offset(6)));
        result.ShouldContain(new TopicPartitionOffset(new TopicPartition("topic1", new Partition(1)), new Offset(11)));
    }
}

