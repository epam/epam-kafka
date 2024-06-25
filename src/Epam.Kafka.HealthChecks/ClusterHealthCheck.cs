// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Confluent.SchemaRegistry;
using Epam.Kafka.Internals;
using Epam.Kafka.Options;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Epam.Kafka.HealthChecks;

internal sealed class ClusterHealthCheck : IHealthCheck, IObserver<Error>
{
    private readonly IKafkaFactory _kafkaFactory;
    private readonly IOptionsMonitor<ClusterHealthCheckOptions> _optionsMonitor;
    private readonly IOptionsMonitor<KafkaClusterOptions> _clusterOptionsMonitor;
    private readonly List<Error> _errors = new();

    public const string NamePrefix = "Epam.Kafka.Clusters.";

    public ClusterHealthCheck(
        IKafkaFactory kafkaFactory,
        IOptionsMonitor<ClusterHealthCheckOptions> optionsMonitor,
        IOptionsMonitor<KafkaClusterOptions> clusterOptionsMonitor)
    {
        this._kafkaFactory = kafkaFactory ?? throw new ArgumentNullException(nameof(kafkaFactory));
        this._optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
        this._clusterOptionsMonitor = clusterOptionsMonitor ?? throw new ArgumentNullException(nameof(clusterOptionsMonitor));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        string description = "Not used by application.";
        HealthStatus status = HealthStatus.Healthy;

        string name = context.Registration.Name.Substring(NamePrefix.Length);

        ClusterHealthCheckOptions options = this._optionsMonitor.Get(name);

        if (options.IncludeUnused || this._kafkaFactory is not KafkaFactory kf || kf.UsedClusters.Contains(name))
        {
            description = "AdminClient: ";

            if (options.SkipAdminClient)
            {
                description += "check skipped.";
            }
            else
            {
#pragma warning disable CA1031 // Not applicable for this health checks

                try
                {
                    using IClient client = this._kafkaFactory.GetOrCreateClient(name);

                    using IDisposable subscription = ((IObservable<Error>)client).Subscribe(this);

                    using IAdminClient adminClient = client.CreateDependentAdminClient();

                    await adminClient
                        .DescribeClusterAsync(new DescribeClusterOptions
                        { RequestTimeout = context.Registration.Timeout })
                        .ConfigureAwait(false);

                    description += "OK.";
                }
                catch (Exception e)
                {
                    status = context.Registration.FailureStatus;

                    if (this._errors.Count > 0)
                    {
                        description += string.Join(", ", this._errors) + ".";
                    }
                    else
                    {
                        description += $"{e.Message}.";
                    }
                }
            }

            if (options.SkipSchemaRegistry)
            {
                description += " SchemaRegistry: check skipped.";
            }
            else
            {
                try
                {
                    if (this._clusterOptionsMonitor.Get(name).SchemaRegistryConfig.Any())
                    {
                        ISchemaRegistryClient sr = this._kafkaFactory.GetOrCreateSchemaRegistryClient(name);
                        await sr.GetCompatibilityAsync().ConfigureAwait(false);
                        description += " SchemaRegistry: OK.";
                    }
                    else
                    {
                        description += " SchemaRegistry: not configured.";
                    }
                }
                catch (Exception e)
                {
                    status = context.Registration.FailureStatus;
                    description += $" SchemaRegistry: {e.Message}.";
                }
            }

#pragma warning restore CA1031
        }

        return new HealthCheckResult(status, description);
    }

    public void OnNext(Error value)
    {
        this._errors.Add(value);
    }

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
        this._errors.Clear();
    }
}