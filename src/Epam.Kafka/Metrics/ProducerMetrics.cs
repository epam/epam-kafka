// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;

namespace Epam.Kafka.Metrics;

internal sealed class ProducerMetrics : CommonMetrics
{
#pragma warning disable CA2000 // Meter will be disposed in base class
    protected override void Initialize(Func<string, IEnumerable<KeyValuePair<string, object?>>?, Meter> meterFactory)
    {
        base.Initialize(meterFactory);

        Meter meter = meterFactory(Statistics.TransactionMeterName, null);

        meter.CreateObservableGauge("epam_kafka_stats_eos_txn_state", () => (long)this.Value!.ExactlyOnceSemantics.TransactionState,
            null, "Transaction state");

        meter.CreateObservableGauge("epam_kafka_stats_eos_idemp_state", () => (long)this.Value!.ExactlyOnceSemantics.IdempotentState,
            null, "Idempotent state");
    }
#pragma warning restore CA2000
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