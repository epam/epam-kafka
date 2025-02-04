// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

using Confluent.Kafka;

namespace Epam.Kafka.Metrics;

internal sealed class ProducerMetrics : CommonMetrics
{
    private const string TransactionTagName = "Transaction";
    private const string TrStateTagName = "TrState";
    private const string IdempStateTagName = "IdempState";

    private readonly ProducerConfig _config;

    public ProducerMetrics(ProducerConfig config)
    {
        this._config = config ?? throw new ArgumentNullException(nameof(config));
    }

#pragma warning disable CA2000 // Meter will be disposed in base class
    protected override void Initialize(Func<string, IEnumerable<KeyValuePair<string, object?>>?, Meter> meterFactory)
    {
        base.Initialize(meterFactory);

        Meter meter = meterFactory(Statistics.TransactionMeterName, null);

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
                            new KeyValuePair<string, object?>(TrStateTagName, v.ProducerTransaction.TransactionState),
                            new KeyValuePair<string, object?>(TransactionTagName, this._config.TransactionalId)
                        }), 1);
                }

                return Empty;
            }, "seconds", "Transaction state age seconds");
        }

        meter.CreateObservableGauge("epam_kafka_stats_eos_idemp_age", () =>
        {
            Statistics? v = this.Value;

            // idempotent producer enabled
            if (v is { ProducerTransaction.IdempotentAgeMilliseconds: > 0 })
            {
                return Enumerable.Repeat(new Measurement<long>(v.ProducerTransaction.IdempotentAgeMilliseconds / 1000,
                    new[]
                    {
                        new KeyValuePair<string, object?>(IdempStateTagName, v.ProducerTransaction.IdempotentState),
                    }), 1);
            }

            return Empty;
        }, "seconds", "Idempotent state age seconds");
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