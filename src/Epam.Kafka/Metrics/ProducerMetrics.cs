// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

using Confluent.Kafka;

namespace Epam.Kafka.Metrics;

internal sealed class ProducerMetrics : CommonMetrics
{
    private const string EnqueueTagName = "Enqueue";
    private const string TransactionTagName = "Transaction";
    private const string StateTagName = "State";

    private readonly ProducerConfig _config;

    public ProducerMetrics(ProducerConfig config)
    {
        this._config = config ?? throw new ArgumentNullException(nameof(config));
    }

    protected override void Initialize(Meter meter, Meter topParMeter, Meter transactionMeter)
    {
        base.Initialize(meter, topParMeter, transactionMeter);

        transactionMeter.CreateObservableGauge("epam_kafka_stats_eos_txn_age", () =>
        {
            Statistics? v = this.Value;

            if (v != null && !string.IsNullOrWhiteSpace(this._config.TransactionalId))
            {
                return Enumerable.Repeat(new Measurement<long>(v.ProducerTransaction.TransactionAgeMilliseconds / 1000,
                    new[]
                    {
                        new KeyValuePair<string, object?>(EnqueueTagName, v.ProducerTransaction.EnqAllowed),
                        new KeyValuePair<string, object?>(StateTagName, v.ProducerTransaction.TransactionState),
                        new KeyValuePair<string, object?>(TransactionTagName, this._config.TransactionalId)
                    }), 1);
            }

            return Empty;
        }, "seconds", "Transactional producer state age seconds");
    }

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