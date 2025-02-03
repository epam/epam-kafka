// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

using Confluent.Kafka;

namespace Epam.Kafka.Metrics;

internal sealed class ProducerMetrics : CommonMetrics
{
    private const string TransactionTagName = "Transaction";
    private const string StateTagName = "State";

    private readonly ProducerConfig _config;

    public ProducerMetrics(ProducerConfig config)
    {
        this._config = config ?? throw new ArgumentNullException(nameof(config));
    }

#pragma warning disable CA2000 // Meter will be disposed in base class
    protected override IEnumerable<Meter> Initialize(KeyValuePair<string, object?>[] topLevelTags)
    {
        foreach (Meter m in base.Initialize(topLevelTags))
        {
            yield return m;
        }

        Meter meter = new(Statistics.TransactionMeterName, null, topLevelTags);

        if (!string.IsNullOrWhiteSpace(this._config.TransactionalId))
        {
            meter.CreateObservableGauge("epam_kafka_stats_eos_txn_age", () =>
            {
                Statistics? v = this.Value;

                if (v != null)
                {
                    return Enumerable.Repeat(new Measurement<long>(v.ProducerTransaction.TransactionAgeMilliseconds / 1000,
                        new[]
                        {
                            new KeyValuePair<string, object?>(StateTagName, v.ProducerTransaction.TransactionState),
                            new KeyValuePair<string, object?>(TransactionTagName, this._config.TransactionalId)
                        }), 1);
                }

                return Empty;
            }, "seconds", "Transaction state age seconds");
        }

        meter.CreateObservableGauge("epam_kafka_stats_eos_idemp_age", () =>
        {
            Statistics? v = this.Value;

            if (v != null && !string.IsNullOrWhiteSpace(this._config.TransactionalId))
            {
                return Enumerable.Repeat(new Measurement<long>(v.ProducerTransaction.IdempotentAgeMilliseconds / 1000,
                    new[]
                    {
                        new KeyValuePair<string, object?>(StateTagName, v.ProducerTransaction.IdempotentState),
                    }), 1);
            }

            return Empty;
        }, "seconds", "Idempotent state age seconds");

        yield return meter;
    }
#pragma warning restore CA2000 // Meter will be disposed in base class
    protected override long GetTxRxMsg(Statistics value)
    {
        return value.TransmittedMessagesTotal;
    }

    protected override long GetTxRx(Statistics value)
    {
        return value.TransmittedRequestsTotal;
    }

    protected override long GetTxRxBytes(Statistics value)
    {
        return value.TransmittedBytesTotal;
    }
}