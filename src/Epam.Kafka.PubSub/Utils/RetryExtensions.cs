// Copyright © 2024 EPAM Systems

namespace Epam.Kafka.PubSub.Utils;

internal static class RetryExtensions
{
    private static readonly object BatchKey = new();
    private static readonly object PipelineKey = new();

    public static void DoNotRetryBatch(this Exception exception)
    {
        exception.Data[BatchKey] = null;
    }

    public static void DoNotRetryPipeline(this Exception exception)
    {
        exception.Data[PipelineKey] = null;
        exception.Data[BatchKey] = null;
    }

    public static bool RetryBatchAllowed(this Exception exception)
    {
        return !exception.Data.Contains(BatchKey);
    }

    public static bool RetryPipelineAllowed(this Exception exception)
    {
        return !exception.Data.Contains(PipelineKey);
    }
}