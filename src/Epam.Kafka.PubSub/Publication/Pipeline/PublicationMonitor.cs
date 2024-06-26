// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Common;
using Epam.Kafka.PubSub.Common.Pipeline;

using System.Collections.Concurrent;

namespace Epam.Kafka.PubSub.Publication.Pipeline;

/// <summary>
///     Monitor to check publication pipeline status.
/// </summary>
public class PublicationMonitor : PubSubMonitor<PublicationBatchResult>
{
    /// <summary>
    /// Name prefix for all publications
    /// </summary>
    public const string Prefix = "Epam.Kafka.Publication";

    internal PublicationMonitor(PubSubContext context, string name) : base(context, BuildFullName(name))
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

    internal bool TryRegisterTransactionId(ProducerConfig config, out string? existingName)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        existingName = null;
        ConcurrentDictionary<string, PublicationMonitor> ids = this.Context.TransactionIds;
        string id = config.TransactionalId!;

        bool result = ids.TryAdd(id, this) || ids.TryUpdate(id, this, this);

        if (!result)
        {
            existingName = ids[id].Name;
        }

        return result;
    }
}