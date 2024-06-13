// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Confluent.SchemaRegistry;

using Epam.Kafka.PubSub.Common.Options;

using Microsoft.Extensions.Options;

namespace Epam.Kafka.PubSub.Publication.Options;

/// <summary>
///     Options to configure publication service.
/// </summary>
public sealed class PublicationOptions : PubSubOptions, IOptions<PublicationOptions>
{
    internal readonly ProducerPartitioner Partitioner = new();
    internal Func<Lazy<ISchemaRegistryClient>, object>? KeySerializer;
    internal Func<Lazy<ISchemaRegistryClient>, object>? ValueSerializer;

    /// <summary>
    ///     Initialize the <see cref="PubSubOptions" /> options.
    /// </summary>
    public PublicationOptions()
    {
        this.BatchEmptyTimeout = TimeSpan.FromSeconds(5);
    }

    /// <summary>
    ///     Topic name for producer. Mandatory setting.
    ///     <remarks>
    ///         Publication services use this value as default one if message specific value not provided in
    ///         <see cref="TopicMessage{TKey,TValue}.Topic" />
    ///     </remarks>
    /// </summary>
    public string? DefaultTopic { get; set; }

    /// <summary>
    ///     The logical name for <see cref="IProducer{TKey,TValue}" /> to create it using <see cref="IKafkaFactory" />
    /// </summary>
    public string? Producer { get; set; }

    /// <summary>
    ///     Whether serialization preprocessor (which serialize all messages before attempting to produce first one to be able
    ///     to handle error earlier) is enabled.
    ///     If null (default) it will be enabled implicitly if transactional producer or custom serializer is used.
    /// </summary>
    public bool? SerializationPreprocessor { get; set; } = true;

    PublicationOptions IOptions<PublicationOptions>.Value => this;
}