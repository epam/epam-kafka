// Copyright © 2024 EPAM Systems

using Microsoft.Extensions.Logging;

namespace Epam.Kafka.PubSub.EntityFrameworkCore;

internal static partial class LogExtensions
{
    [LoggerMessage(
        EventId = 89,
        EventName = "PublicationEntityDetached",
        Level = LogLevel.Warning,
        Message = "Publication entity ({EntityKey}) of type {EntityType} detached on {Stage}.")]
    public static partial void PublicationEntityDetached(this ILogger logger, Exception exception, string stage,
        object? entityKey, Type entityType);
}