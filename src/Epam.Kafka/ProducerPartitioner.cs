// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

namespace Epam.Kafka;

/// <summary>
///     Configuration for default and topic specific partitioner.
/// </summary>
public class ProducerPartitioner
{
    /// <summary>
    ///     The default <see cref="PartitionerDelegate" /> to be passed to
    ///     <see cref="ProducerBuilder{TKey,TValue}.SetDefaultPartitioner" /> method.
    /// </summary>
    public PartitionerDelegate? Default { get; set; }

    /// <summary>
    ///     The dictionary to specify <see cref="PartitionerDelegate" /> for particular topic name. Will be used with
    ///     <see cref="ProducerBuilder{TKey,TValue}.SetPartitioner" /> method.
    /// </summary>
    public IDictionary<string, PartitionerDelegate> TopicSpecific { get; } =
        new Dictionary<string, PartitionerDelegate>();

    /// <summary>
    ///     Apply partitioner configuration to <see cref="ProducerBuilder{TKey,TValue}" />
    /// </summary>
    /// <typeparam name="TKey">The producer key type.</typeparam>
    /// <typeparam name="TValue">The producer value type.</typeparam>
    /// <param name="producerBuilder">The builder instance to which configuration should be applied.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void Apply<TKey, TValue>(ProducerBuilder<TKey, TValue> producerBuilder)
    {
        if (producerBuilder == null)
        {
            throw new ArgumentNullException(nameof(producerBuilder));
        }

        if (this.Default != null)
        {
            producerBuilder.SetDefaultPartitioner(this.Default);
        }

        foreach (KeyValuePair<string, PartitionerDelegate> kvp in this.TopicSpecific)
        {
            producerBuilder.SetPartitioner(kvp.Key, kvp.Value);
        }
    }
}