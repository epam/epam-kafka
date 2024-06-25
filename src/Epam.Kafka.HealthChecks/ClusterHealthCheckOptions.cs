// Copyright © 2024 EPAM Systems

using Microsoft.Extensions.Options;

namespace Epam.Kafka.HealthChecks;

/// <summary>
/// Options for kafka cluster health checks.
/// </summary>
public sealed class ClusterHealthCheckOptions : IOptions<ClusterHealthCheckOptions>
{
    /// <summary>
    /// Whether schema registry check should be skipped. Default <code>false</code>.
    /// </summary>
    public bool SkipSchemaRegistry { get; set; }

    /// <summary>
    /// Whether admin client check should be skipped. Default <code>false</code>.
    /// </summary>
    public bool SkipAdminClient { get; set; }

    /// <summary>
    /// Whether cluster will be checked even if was not used at least 1 time by default <see cref="IKafkaFactory"/> implementation. Default <code>false</code>.
    /// </summary>
    public bool IncludeUnused { get; set; }

    ClusterHealthCheckOptions IOptions<ClusterHealthCheckOptions>.Value => this;
}