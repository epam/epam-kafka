// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common;
using Epam.Kafka.PubSub.Common.Pipeline;

namespace Epam.Kafka.PubSub.Subscription.Pipeline;

/// <summary>
///     Monitor to check subscription pipeline status.
/// </summary>
public class SubscriptionMonitor : PubSubMonitor<SubscriptionBatchResult>
{
    /// <summary>
    /// Name prefix for all subscriptions
    /// </summary>
    public const string Prefix = "Epam.Kafka.Subscription";

    internal SubscriptionMonitor(PubSubContext context, string name) : base(context, BuildFullName(name))
    {
    }

    internal static string BuildFullName(string name)
    {
        return $"{Prefix}.{name}";
    }

    internal override void HandleResult(SubscriptionBatchResult batchResult)
    {
        if (batchResult is SubscriptionBatchResult.Empty or SubscriptionBatchResult.Processed)
        {
            this.PipelineRetryIteration = 0;
        }
    }
}