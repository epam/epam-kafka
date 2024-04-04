// Copyright © 2024 EPAM Systems

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Epam.Kafka.Sample;

public class ConsoleHealthCheckPublisher : IHealthCheckPublisher
{

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        foreach (KeyValuePair<string, HealthReportEntry> entry in report.Entries)
        {
            Console.WriteLine($"{entry.Key} {entry.Value.Status:G} {entry.Value.Description}");
        }

        return Task.CompletedTask;
    }
}