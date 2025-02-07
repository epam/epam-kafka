// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Publication.Pipeline;
using Epam.Kafka.PubSub.Subscription.Options;
using Epam.Kafka.PubSub.Subscription.Pipeline;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Epam.Kafka.PubSub.Common.Pipeline;

/// <summary>
///     Representation of Subscription or Publication processing state.
/// </summary>
public abstract class PipelineMonitor
{
    /// <summary>
    ///     Name of the <see cref="Meter" /> with health metrics.
    /// </summary>
    public const string HealthMeterName = "Epam.Kafka.PubSub.Health";

    /// <summary>
    ///     Name of the <see cref="ObservableGauge{T}" /> with health metrics. Int values corresponds to
    ///     <see cref="HealthStatus" />.
    /// </summary>
    public const string HealthGaugeName = "epam_kafka_pubsub_health";

    /// <summary>
    ///     Name of the <see cref="Meter" /> with last batch result metrics.
    /// </summary>
    public const string StatusMeterName = "Epam.Kafka.PubSub.Status";

    /// <summary>
    ///     Name of the <see cref="ObservableGauge{T}" /> with pipeline status metrics. Int values corresponds to
    ///     <see cref="PipelineStatus" />.
    /// </summary>
    public const string StatusPipelineGaugeName = "epam_kafka_pubsub_status_pipeline";

    /// <summary>
    ///     Name of the <see cref="ObservableGauge{T}" /> with batch status metrics. Int values corresponds to
    ///     <see cref="BatchStatus" />.
    /// </summary>
    public const string StatusBatchGaugeName = "epam_kafka_pubsub_status_batch";

    /// <summary>
    /// Age of batch status (elapsed milliseconds since last batch status change)
    /// </summary>
    public const string StatusBatchAgeGaugeName = "epam_kafka_pubsub_status_age_batch";

    /// <summary>
    ///     Name of the <see cref="ObservableGauge{T}" /> with last batch result metrics. Int values corresponds to
    ///     <see cref="SubscriptionBatchResult" /> for subscription and <see cref="PublicationBatchResult" /> for publication.
    /// </summary>
    public const string StatusResultGaugeName = "epam_kafka_pubsub_status_result";

    internal PipelineMonitor(PubSubContext context, string name)
    {
        this.Context = context ?? throw new ArgumentNullException(nameof(context));
        this.FullName = name ?? throw new ArgumentNullException(nameof(name));
        this.Name = this.FullName.Split('.').Last();
        this.NamePlaceholder = new Dictionary<string, string> { { "<name>", this.Name } };
    }

    /// <summary>
    /// The <see cref="PubSubContext"/>
    /// </summary>
    internal PubSubContext Context { get; }

    internal string FullName { get; }

    /// <summary>
    ///     Name used to add in <see cref="KafkaBuilderExtensions" />.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     The pipeline status <see cref="PipelineStatus" />.
    /// </summary>
    public StatusDetails<PipelineStatus> Pipeline { get; } = new(PipelineStatus.None);

    /// <summary>
    ///     The batch processing status <see cref="BatchStatus" />
    /// </summary>
    public StatusDetails<BatchStatus> Batch { get; } = new(BatchStatus.None);

    /// <summary>
    ///     Number of sequential pipeline errors without at least one successful batch.
    /// </summary>
    public int PipelineRetryIteration { get; internal set; }

    internal IReadOnlyDictionary<string, string> NamePlaceholder { get; }

    internal bool TryRegisterTransactionId(ProducerConfig config, out string? existingName)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        existingName = null;
        ConcurrentDictionary<string, PipelineMonitor> ids = this.Context.TransactionIds;
        string id = config.TransactionalId!;

        bool result = ids.TryAdd(id, this) || ids.TryUpdate(id, this, this);

        if (!result)
        {
            existingName = ids[id].Name;
        }

        return result;
    }

    internal bool TryRegisterGroupId(ConsumerConfig config, SubscriptionOptions options, out string? msg)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        msg = null;
        bool result = true;

        if (options.IsTopicNameWithPartition(out Type? storageType))
        {
            ConcurrentDictionary<Tuple<string, TopicPartition, Type>, PipelineMonitor> ids = this.Context.PartitionHandlers;

            foreach (TopicPartition tp in options.GetTopicPartitions())
            {
                Tuple<string, TopicPartition, Type> key = new(config.GroupId, tp, storageType!);

                result = ids.TryAdd(key, this) || ids.TryUpdate(key, this, this);

                if (!result)
                {
                    msg = $" Already used for '{tp}' topic partition with external state storage of type '{storageType}' in '{ids[key].Name}' subscription.";
                }
            }
        }
        else
        {
            ConcurrentDictionary<Tuple<string, string>, Type> ids = this.Context.TopicHandlers;

            foreach (string t in options.GetTopicNames())
            {
                Tuple<string, string> key = new(config.GroupId, t);

                result = ids.TryAdd(key, options.HandlerType!) || ids.TryUpdate(key, options.HandlerType!, options.HandlerType!);

                if (!result)
                {
                    msg = $"Already used for '{t}' topic with handler of type '{ids[key].Name}'.";
                }
            }
        }

        return result;
    }
}