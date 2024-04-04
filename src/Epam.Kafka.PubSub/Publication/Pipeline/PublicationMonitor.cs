// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Pipeline;

namespace Epam.Kafka.PubSub.Publication.Pipeline;

/// <summary>
///     Monitor to check publication pipeline status.
/// </summary>
public class PublicationMonitor : PubSubMonitor<PublicationBatchResult>
{
    internal const string Prefix = "Epam.Kafka.Publication";

    internal PublicationMonitor(string name) : base(BuildFullName(name))
    {
    }

    internal static string BuildFullName(string name)
    {
        return $"{Prefix}.{name}";
    }

    internal override void HandleResult(PublicationBatchResult batchResult)
    {
        if (batchResult is PublicationBatchResult.Empty or PublicationBatchResult.Processed
            or PublicationBatchResult.ProcessedPartial)
        {
            this.PipelineRetryIteration = 0;
        }
    }
}