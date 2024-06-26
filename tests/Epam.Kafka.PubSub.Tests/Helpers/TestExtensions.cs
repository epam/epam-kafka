// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Publication;
using Epam.Kafka.Tests.Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Epam.Kafka.PubSub.Tests.Helpers;

public static class TestExtensions
{
    public static KeyValuePair<string, DeliveryReport> ToReport(this TopicMessage<string, TestEntityKafka> message,
        Offset offset,
        string? topicName = null,
        Partition? partition = null,
        ErrorCode errorCode = ErrorCode.NoError,
        PersistenceStatus status = PersistenceStatus.Persisted)
    {
        return new KeyValuePair<string, DeliveryReport>(message.Key,
            new DeliveryReport(message.Topic ?? topicName ?? string.Empty, partition ?? 0, offset, new Error(errorCode),
                status, Timestamp.Default));
    }

    public static ConsumeResult<string, TestEntityKafka> ToConsumeResult(this TestEntityKafka entity,
        TopicPartitionOffset tpo)
    {
        return new ConsumeResult<string, TestEntityKafka>
        { TopicPartitionOffset = tpo, Message = entity.ToMessage(tpo.Topic) };
    }

    public static Task RunBackgroundServices(this TestWithServices test)
    {
        return Task.WhenAny(test.ServiceProvider.GetServices<IHostedService>().OfType<BackgroundService>().Select(x =>
        {
            x.StartAsync(test.Ctc.Token);
            return x.ExecuteTask!;
        })).Result;
    }

    public static TopicMessage<string, TestEntityKafka> ToMessage(this TestEntityKafka entity, string? topicName = null)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        return new TopicMessage<string, TestEntityKafka> { Key = entity.Id, Value = entity, Topic = topicName };
    }
}