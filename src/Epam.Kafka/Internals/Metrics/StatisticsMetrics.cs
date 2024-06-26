﻿// Copyright © 2024 EPAM Systems

namespace Epam.Kafka.Internals.Metrics;

internal abstract class StatisticsMetrics : IObserver<Statistics>
{
    protected const string NamePrefix = "epam_kafka_statistics";

    private bool _created;
    protected Statistics Latest { get; private set; } = null!;
    protected KeyValuePair<string, object?>[] Tags { get; private set; } = null!;
    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(Statistics value)
    {
        this.Latest = value;

        if (this._created == false)
        {
            const int len = 10;

            bool trim = value.Name.StartsWith(value.ClientId, StringComparison.OrdinalIgnoreCase) &&
                        value.Name.Length - value.ClientId.Length > len;

            this.Tags = new[]
            {
                new KeyValuePair<string, object?>("client", value.ClientId),
                new KeyValuePair<string, object?>("instance", trim ? value.Name.Substring(value.ClientId.Length + len) : value.Name),
                new KeyValuePair<string, object?>("type", value.Type),
            };
            this.Create();
            this._created = true;
        }
    }

    protected abstract void Create();
}