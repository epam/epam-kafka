// Copyright © 2024 EPAM Systems

namespace Epam.Kafka.PubSub.Common.Pipeline;

internal static class BatchResult
{
    public const int Error = 1;
    public const int Empty = 2;
    public const int Processed = 3;
}