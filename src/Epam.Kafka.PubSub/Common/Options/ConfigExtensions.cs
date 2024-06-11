// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

#if !NET6_0_OR_GREATER
using Epam.Kafka.Internals;
#endif

using Epam.Kafka.PubSub.Common.Pipeline;

using System.Text.RegularExpressions;

namespace Epam.Kafka.PubSub.Common.Options;

internal static class ConfigExtensions
{
    public static void UpdateClientIdIfNeeded(this ClientConfig config, PubSubOptions options, PipelineMonitor monitor)
    {
        if (!string.IsNullOrWhiteSpace(options.ConfigOverrideClientId))
        {
            config.ClientId = options.ConfigOverrideClientId!
                .Replace("<pubSubFullName>", monitor.FullName, StringComparison.OrdinalIgnoreCase)
                .Replace("<pubSubFullNameToLower>", monitor.FullName.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)
                .Replace("<pubSubName>", monitor.Name, StringComparison.OrdinalIgnoreCase)
                .Replace("<pubSubNameToLower>", monitor.Name.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
        }
    }
}