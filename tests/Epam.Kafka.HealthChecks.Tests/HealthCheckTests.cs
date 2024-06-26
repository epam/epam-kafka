// Copyright © 2024 EPAM Systems

using Epam.Kafka.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.HealthChecks.Tests;

public class HealthCheckTests : TestWithServices
{

    public HealthCheckTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void AddCheckExceptions()
    {
        Assert.Throws<ArgumentNullException>(() => HealthCheckExtensions.WithHealthCheck(null!));

        Assert.Throws<ArgumentException>(() =>
                this.Services.AddKafka(false).WithClusterConfig(string.Empty).WithHealthCheck()).Message
            .ShouldContain("empty cluster name");
    }

    [Fact]
    public async Task CheckUnusedCluster()
    {
        this.Services.AddKafka(false).WithClusterConfig("Sandbox").WithHealthCheck()
            .Configure(x =>
            {
                x.SkipAdminClient = true;
            });

        var hc = this.ServiceProvider.GetRequiredService<ClusterHealthCheck>();

        HealthCheckResult result = await hc.CheckHealthAsync(new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("Epam.Kafka.Clusters.Sandbox", hc, HealthStatus.Unhealthy, null,
                TimeSpan.FromSeconds(20))
        });

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("Not used by application");
    }

    [Fact]
    public async Task CheckCluster()
    {
        this.Services.AddKafka(false)
            .WithClusterConfig("Sandbox").Configure(x =>
            {
                x.ClientConfig.BootstrapServers = "localhost:9092";
            })
            .WithHealthCheck().Configure(x =>
            {
                x.IncludeUnused = true;
                x.SkipAdminClient = true;
            });

        var hc = this.ServiceProvider.GetRequiredService<ClusterHealthCheck>();

        HealthCheckResult result = await hc.CheckHealthAsync(new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("Epam.Kafka.Clusters.Sandbox", hc, HealthStatus.Unhealthy, null,
                TimeSpan.FromSeconds(20))
        });

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description!.ShouldContain("AdminClient: check skipped. SchemaRegistry: not configured.");
    }

    [Fact]
    public async Task CheckClusterErrors()
    {
        this.Services.AddKafka(false)
            .WithClusterConfig("Sandbox").Configure(x =>
            {
                x.ClientConfig.BootstrapServers = "any-not-existing-value:9092";
                x.SchemaRegistryConfig.Url = "any-not-existing-value:8080";
            })
            .WithHealthCheck().Configure(x =>
            {
                x.IncludeUnused = true;
            });

        var hc = this.ServiceProvider.GetRequiredService<ClusterHealthCheck>();

        HealthCheckResult result = await hc.CheckHealthAsync(new HealthCheckContext
        {
            Registration = new HealthCheckRegistration("Epam.Kafka.Clusters.Sandbox", hc, HealthStatus.Unhealthy, null, TimeSpan.FromSeconds(3))
        });

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description!.ShouldContain("AdminClient: any-not-existing-value:9092");
        result.Description!.ShouldContain("SchemaRegistry:");
        result.Description!.Substring(result.Description!.IndexOf("SchemaRegistry:",StringComparison.Ordinal)).ShouldContain("SchemaRegistry: [http://any-not-existing-value:8080/] HttpRequestException");
    }
}