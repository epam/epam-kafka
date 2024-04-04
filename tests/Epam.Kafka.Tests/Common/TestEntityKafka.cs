// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using System.Text;

namespace Epam.Kafka.Tests.Common;

public class TestEntityKafka
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name => $"Name for {this.Id}";

    public Message<string, byte[]> ToBytesMessage()
    {
        return new Message<string, byte[]> { Key = this.Id, Value = this.GetBytesId() };
    }

    public byte[] GetBytesId()
    {
        return Encoding.UTF8.GetBytes(this.Id);
    }

    public override string ToString()
    {
        return $"TestEntityKafka {this.Id}";
    }

    public static Dictionary<TestEntityKafka, TopicPartitionOffset> CreateDefault(string topic, Partition partition,
        int offset)
    {
        return Enumerable.Range(0, 5).Select(i =>
                new KeyValuePair<TestEntityKafka, TopicPartitionOffset>(new TestEntityKafka(),
                    new TopicPartitionOffset(topic, partition, offset + i)))
            .ToDictionary();
    }
}