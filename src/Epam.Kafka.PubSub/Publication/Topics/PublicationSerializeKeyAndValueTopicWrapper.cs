// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Common.Pipeline;
using Epam.Kafka.PubSub.Utils;

using Microsoft.Extensions.Logging;

using System.Diagnostics;

namespace Epam.Kafka.PubSub.Publication.Topics;

internal class PublicationSerializeKeyAndValueTopicWrapper<TKey, TValue> : IPublicationTopicWrapper<TKey, TValue>
{
    private readonly PublicationTopicWrapper<byte[], byte[]> _inner;
    private readonly ISerializer<TKey> _keySerializer;
    private readonly IPublicationTopicWrapperOptions _options;
    private readonly ISerializer<TValue> _valueSerializer;

    public PublicationSerializeKeyAndValueTopicWrapper(
        IKafkaFactory kafkaFactory,
        PipelineMonitor monitor,
        ProducerConfig config,
        IPublicationTopicWrapperOptions options,
        ILogger logger,
        ISerializer<TKey>? keySerializer,
        ISerializer<TValue>? valueSerializer)
    {
        this._options = options ?? throw new ArgumentNullException(nameof(options));

        this._keySerializer = keySerializer ??
                              (SerializationHelper.TryGetDefaultSerializer(out ISerializer<TKey>? ks)
                                  ? ks!
                                  : throw new ArgumentNullException(nameof(keySerializer),
                                      $"Null serializer for key of type {typeof(TKey)}"));

        this._valueSerializer = valueSerializer ??
                                (SerializationHelper.TryGetDefaultSerializer(out ISerializer<TValue>? vs)
                                    ? vs!
                                    : throw new ArgumentNullException(nameof(keySerializer),
                                        $"Null serializer for value of type {typeof(TValue)}"));

        this._inner = new PublicationTopicWrapper<byte[], byte[]>(kafkaFactory, monitor, config, options, logger, null, null);
    }

    public bool Disposed => this._inner.Disposed;

    public bool RequireTransaction => this._inner.RequireTransaction;
    public DateTimeOffset? TransactionEnd => this._inner.TransactionEnd;

    public void Dispose()
    {
        this._inner.Dispose();
    }

    public void CommitTransactionIfNeeded(ActivityWrapper apm)
    {
        this._inner.CommitTransactionIfNeeded(apm);
    }

    public void AbortTransactionIfNeeded(ActivityWrapper apm)
    {
        this._inner.AbortTransactionIfNeeded(apm);
    }

    public IDictionary<TopicMessage<TKey, TValue>, DeliveryReport> Produce(
        IReadOnlyCollection<TopicMessage<TKey, TValue>> items,
        ActivityWrapper activitySpan,
        Stopwatch stopwatch,
        TimeSpan handlerTimeout,
        CancellationToken cancellationToken)
    {
        Dictionary<TopicMessage<byte[], byte[]>, TopicMessage<TKey, TValue>> serialized = new(items.Count);

        Dictionary<TopicMessage<TKey, TValue>, DeliveryReport> result = new(items.Count);

        using (activitySpan.CreateSpan("serialize"))
        {
            foreach (TopicMessage<TKey, TValue> item in items)
            {
                item.Topic ??= this._options.GetDefaultTopic();

                Headers headers = item.Headers ?? new();

                var keyContext = new SerializationContext(MessageComponentType.Key, item.Topic, headers);
                var valueContext = new SerializationContext(MessageComponentType.Value, item.Topic, headers);

                ErrorCode code = ErrorCode.Local_KeySerialization;

#pragma warning disable CA1031 // Catch any errors for serialization and generate appropriate report
                try
                {
                    byte[] key = this._keySerializer.Serialize(item.Key, keyContext);

                    code = ErrorCode.Local_ValueSerialization;
                    byte[] value = this._valueSerializer.Serialize(item.Value, valueContext);

                    serialized.Add(
                        new TopicMessage<byte[], byte[]>
                        {
                            Key = key,
                            Value = value,
                            Headers = item.Headers,
                            Timestamp = item.Timestamp,
                            Topic = item.Topic
                        }, item);
                }
                catch (Exception e)
                {
                    result.Add(item,
                        new DeliveryReport(keyContext.Topic,
                            Partition.Any,
                            Offset.Unset,
                            new Error(code, e.ToString()),
                            PersistenceStatus.NotPersisted,
                            Timestamp.Default));
                }
#pragma warning restore CA1031
            }
        }

        // if all messages were serialized proceed with producing, otherwise report errors
        if (serialized.Count > 0 && result.Count == 0)
        {
            IDictionary<TopicMessage<byte[], byte[]>, DeliveryReport> innerResult =
                this._inner.Produce(serialized.Keys, activitySpan, stopwatch, handlerTimeout, cancellationToken);

            foreach (KeyValuePair<TopicMessage<byte[], byte[]>, DeliveryReport> report in innerResult)
            {
                result.Add(serialized[report.Key], report.Value);
            }
        }

        return result;
    }
}