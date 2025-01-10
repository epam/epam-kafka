// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Confluent.SchemaRegistry;

using Epam.Kafka.PubSub.Common.Pipeline;
using Epam.Kafka.PubSub.Utils;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Epam.Kafka.PubSub.Publication.Topics;

internal static class PublicationTopicWrapperExtensions
{
    public static IPublicationTopicWrapper<TKey, TValue> CreatePublicationTopicWrapper<TKey, TValue>(
        this IKafkaFactory kafkaFactory,
        IPublicationTopicWrapperOptions options,
        PipelineMonitor monitor, ILogger? logger = null)
    {
        if (kafkaFactory == null) throw new ArgumentNullException(nameof(kafkaFactory));
        if (monitor == null) throw new ArgumentNullException(nameof(monitor));
        logger ??= NullLogger.Instance;

        var registry = new Lazy<ISchemaRegistryClient>(() =>
            kafkaFactory.GetOrCreateSchemaRegistryClient(options.GetCluster()));

        ISerializer<TKey>? ks;
        ISerializer<TValue>? vs;

        try
        {
            ks = (ISerializer<TKey>?)options.GetKeySerializer()?.Invoke(registry);
            vs = (ISerializer<TValue>?)options.GetValueSerializer()?.Invoke(registry);
        }
        catch (Exception exception)
        {
            exception.DoNotRetryPipeline();
            throw;
        }

        ProducerConfig config = kafkaFactory.CreateProducerConfig(options.GetProducer());

        config = config.Clone(monitor.NamePlaceholder);
        if (config.All(x => x.Key != KafkaConfigExtensions.DotnetLoggerCategoryKey))
        {
            config.SetDotnetLoggerCategory(monitor.FullName);
        }

        bool implicitPreprocessor = ks != null || vs != null || config.TransactionalId != null;

        IPublicationTopicWrapper<TKey, TValue> result = options.GetSerializationPreprocessor() ?? implicitPreprocessor
            ? new PublicationSerializeKeyAndValueTopicWrapper<TKey, TValue>(kafkaFactory, monitor,
                config, options, logger,
                ks, vs)
            : new PublicationTopicWrapper<TKey, TValue>(kafkaFactory, monitor,
                config, options, logger,
                ks, vs);

        return result;
    }
}