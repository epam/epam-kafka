// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Publication.HealthChecks;
using Epam.Kafka.PubSub.Publication.Options;
using Epam.Kafka.PubSub.Publication.Pipeline;
using Epam.Kafka.PubSub.Subscription.HealthChecks;
using Epam.Kafka.PubSub.Subscription.Options;
using Epam.Kafka.PubSub.Subscription.Pipeline;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using System.Text;

namespace Epam.Kafka.PubSub.Common.HealthChecks;

internal class PubSubSummaryHealthCheck : IHealthCheck
{
    public const string Name = "Epam.Kafka.PubSub";

    private readonly PubSubContext _monitors;
    private readonly IOptionsMonitor<PublicationOptions> _pubOptions;
    private readonly IOptionsMonitor<SubscriptionOptions> _subOptions;

    public PubSubSummaryHealthCheck(PubSubContext monitors, IOptionsMonitor<SubscriptionOptions> subOptions,
        IOptionsMonitor<PublicationOptions> pubOptions)
    {
        this._monitors = monitors ?? throw new ArgumentNullException(nameof(monitors));
        this._subOptions = subOptions ?? throw new ArgumentNullException(nameof(subOptions));
        this._pubOptions = pubOptions ?? throw new ArgumentNullException(nameof(pubOptions));
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new())
    {
        static void AppendStatus(StringBuilder stringBuilder, string key, HealthStatus status)
        {
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Append(", ");
            }

            stringBuilder.Append(key);
            stringBuilder.Append(": ");
            stringBuilder.Append(status.ToString("G"));
        }

        StringBuilder sb = new();

        HashSet<HealthStatus> result = new();

        foreach (KeyValuePair<string, SubscriptionMonitor> sub in this._monitors.Subscriptions)
        {
            HealthStatus status = new SubscriptionHealthCheck(this._subOptions, this._monitors, sub.Key)
                .CheckHealthAsync(context, cancellationToken).GetAwaiter().GetResult().Status;
            result.Add(status);

            AppendStatus(sb, sub.Key, status);
        }

        foreach (KeyValuePair<string, PublicationMonitor> pub in this._monitors.Publications)
        {
            HealthStatus status = new PublicationHealthCheck(this._pubOptions, this._monitors, pub.Key)
                .CheckHealthAsync(context, cancellationToken).GetAwaiter().GetResult().Status;
            result.Add(status);

            AppendStatus(sb, pub.Key, status);
        }

        return Task.FromResult(new HealthCheckResult(result.Count == 0
            ? HealthStatus.Healthy
            : result.Count == 1
                ? result.Single()
                : HealthStatus.Degraded, sb.ToString()));
    }
}