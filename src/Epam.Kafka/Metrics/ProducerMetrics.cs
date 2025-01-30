// Copyright © 2024 EPAM Systems

namespace Epam.Kafka.Metrics;

internal sealed class ProducerMetrics : CommonMetrics
{

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