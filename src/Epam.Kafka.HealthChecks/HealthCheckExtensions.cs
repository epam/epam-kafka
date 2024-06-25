// Copyright © 2024 EPAM Systems

using Epam.Kafka.Options;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Epam.Kafka.HealthChecks;

/// <summary>
/// 
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Extensions to configure kafka cluster health checks.
    /// </summary>
    /// <param name="optionsBuilder">The <see cref="OptionsBuilder{KafkaClusterOptions}"/>.</param>
    /// <param name="tags">A list of tags that can be used to filter health checks.</param>
    /// <param name="failureStatus">
    ///     The <see cref="HealthStatus" /> that should be reported when the health check reports a failure. If the provided
    ///     value
    ///     is <c>null</c>, then <see cref="HealthStatus.Unhealthy" /> will be reported.
    /// </param>
    /// <remarks>
    ///     This method will use
    ///     <see
    ///         cref="HealthChecksBuilderAddCheckExtensions.AddCheck{T}(IHealthChecksBuilder,string,HealthStatus?,IEnumerable{string})" />
    ///     to register the health check.
    /// </remarks>
    /// <returns>The health checks options builder for further configuration.</returns>
    public static OptionsBuilder<ClusterHealthCheckOptions> WithHealthCheck(
        this OptionsBuilder<KafkaClusterOptions> optionsBuilder,
        IEnumerable<string>? tags = null,
        HealthStatus? failureStatus = null)
    {
        if (optionsBuilder == null) throw new ArgumentNullException(nameof(optionsBuilder));

        if (string.IsNullOrWhiteSpace(optionsBuilder.Name))
        {
            throw new ArgumentException("Null or empty cluster name.", nameof(optionsBuilder));
        }

        optionsBuilder.Services.TryAddTransient<ClusterHealthCheck>();

        optionsBuilder.Services.AddHealthChecks()
            .AddCheck<ClusterHealthCheck>($"{ClusterHealthCheck.NamePrefix}{optionsBuilder.Name}", failureStatus, tags,
                TimeSpan.FromSeconds(20));

        return optionsBuilder.Services.AddOptions<ClusterHealthCheckOptions>(optionsBuilder.Name);
    }
}
