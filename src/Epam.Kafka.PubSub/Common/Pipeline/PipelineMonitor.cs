// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Publication.Pipeline;
using Epam.Kafka.PubSub.Subscription.Pipeline;

using Microsoft.Extensions.Diagnostics.HealthChecks;

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
    ///     Name of the <see cref="Meter" /> with pipeline and last batch result metrics.
    /// </summary>
    public const string StatusMeterName = "Epam.Kafka.PubSub.Status";

    /// <summary>
    ///     Name of the <see cref="ObservableGauge{T}" /> with pipeline status metrics. Int values corresponds to
    ///     <see cref="PipelineStatus" />.
    /// </summary>
    public const string StatusPipelineGaugeName = "epam_kafka_pubsub_status_pipeline";

    /// <summary>
    ///     Name of the <see cref="ObservableGauge{T}" /> with last batch result metrics. Int values corresponds to
    ///     <see cref="SubscriptionBatchResult" /> for subscription and <see cref="PublicationBatchResult" /> for publication.
    /// </summary>
    public const string StatusResultGaugeName = "epam_kafka_pubsub_status_result";

    internal PipelineMonitor(string name)
    {
        this.FullName = name ?? throw new ArgumentNullException(nameof(name));
        this.Name = this.FullName.Split('.').Last();
    }

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
    ///     Number of sequential pipeline errors without at least one successful batch.
    /// </summary>
    public int PipelineRetryIteration { get; internal set; }
}