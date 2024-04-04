// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Microsoft.Extensions.Logging;

namespace Epam.Kafka.PubSub.Utils;

internal static partial class LogExtensions
{
    public const string IndexedKey = "IndexedKey";

    [LoggerMessage(
        EventId = 0,
        EventName = "WaitBegin",
        Level = LogLevel.Debug,
        Message = "Waiting {Count} item(s) for '{IndexedKey}'.")]
    public static partial void WaitBegin(this ILogger logger, string indexedKey, int count);

    [LoggerMessage(
        EventId = 1,
        EventName = "WaitEnd",
        Level = LogLevel.Debug,
        Message = "Wait end for '{IndexedKey}'.")]
    public static partial void WaitEnd(this ILogger logger, string indexedKey);

    [LoggerMessage(
        EventId = 10,
        EventName = "SubBatchBegin",
        Level = LogLevel.Debug,
        Message = "Batch begin for '{IndexedKey}'. Count: {Count}, From: {From}, To: {To}.")]
    public static partial void SubBatchBegin(this ILogger logger, string indexedKey, int count,
        IEnumerable<TopicPartitionOffset> from, IEnumerable<TopicPartitionOffset> to);

    [LoggerMessage(
        EventId = 11,
        EventName = "BatchEmpty",
        Level = LogLevel.Debug,
        Message = "Batch empty for '{IndexedKey}'.")]
    public static partial void BatchEmpty(this ILogger logger, string indexedKey);

    [LoggerMessage(
        EventId = 12,
        EventName = "ConsumerNotAssigned",
        Level = LogLevel.Debug,
        Message = "Consumer not assigned for '{IndexedKey}'.")]
    public static partial void ConsumerNotAssigned(this ILogger logger, string indexedKey);

    [LoggerMessage(
        EventId = 13,
        EventName = "ConsumerPaused",
        Level = LogLevel.Debug,
        Message = "Consumer paused for '{IndexedKey}'. {TopicPartitions}")]
    public static partial void ConsumerPaused(this ILogger logger, string indexedKey,
        IEnumerable<TopicPartition> topicPartitions);

    [LoggerMessage(
        EventId = 14,
        EventName = "BatchHandlerExecuted",
        Message = "Batch handler executed for {Count} item(s). {HandlerResults}")]
    public static partial void BatchHandlerExecuted(this ILogger logger, int count,
        IEnumerable<KeyValuePair<string, int>> handlerResults, LogLevel level = LogLevel.Information);

    [LoggerMessage(
        EventId = 15,
        EventName = "PubBatchBegin",
        Level = LogLevel.Debug,
        Message = "Batch begin for '{IndexedKey}'. Count: {Count}.")]
    public static partial void PubBatchBegin(this ILogger logger, string indexedKey, int count);

    [LoggerMessage(
        EventId = 20,
        EventName = "OffsetsCommitted",
        Level = LogLevel.Information,
        Message = "Offsets committed for '{IndexedKey}'. {TopicPartitionOffsets}")]
    public static partial void OffsetsCommitted(this ILogger logger, string indexedKey,
        IEnumerable<TopicPartitionOffset> topicPartitionOffsets);

    [LoggerMessage(
        EventId = 21,
        EventName = "OffsetsReset",
        Level = LogLevel.Warning,
        Message = "Offsets reset for '{IndexedKey}'. {TopicPartitionOffsets}")]
    public static partial void OffsetsReset(this ILogger logger, string indexedKey,
        IEnumerable<TopicPartitionOffset> topicPartitionOffsets);

    [LoggerMessage(
        EventId = 22,
        EventName = "PartitionsResumed",
        Level = LogLevel.Information,
        Message = "Partitions resumed for '{IndexedKey}'. {TopicPartitionOffsets}")]
    public static partial void PartitionsResumed(this ILogger logger, string indexedKey,
        IEnumerable<TopicPartitionOffset> topicPartitionOffsets);

    [LoggerMessage(
        EventId = 23,
        EventName = "PartitionsPaused",
        Level = LogLevel.Warning,
        Message = "Partitions paused for '{IndexedKey}'. {TopicPartitions}")]
    public static partial void PartitionsPaused(this ILogger logger, string indexedKey,
        IEnumerable<TopicPartition> topicPartitions);

    [LoggerMessage(
        EventId = 24,
        EventName = "PartitionsAssigned",
        Level = LogLevel.Information,
        Message = "Partitions assigned for '{IndexedKey}'. {TopicPartitionOffsets} {MemberId}")]
    public static partial void PartitionsAssigned(this ILogger logger, string indexedKey, string? memberId,
        IEnumerable<TopicPartitionOffset> topicPartitionOffsets);

    [LoggerMessage(
        EventId = 26,
        EventName = "PartitionsAssignError",
        Level = LogLevel.Error,
        Message = "Partitions assign error for '{IndexedKey}'. {TopicPartitions} {MemberId}")]
    public static partial void PartitionsAssignError(this ILogger logger, Exception exception, string indexedKey,
        string memberId,
        IEnumerable<TopicPartition> topicPartitions);

    [LoggerMessage(
        EventId = 27,
        EventName = "PartitionsRevoked",
        Level = LogLevel.Warning,
        Message = "Partitions revoked for '{IndexedKey}'. {TopicPartitionOffsets} {MemberId}")]
    public static partial void PartitionsRevoked(this ILogger logger, string indexedKey, string memberId,
        IEnumerable<TopicPartitionOffset> topicPartitionOffsets);

    [LoggerMessage(
        EventId = 28,
        EventName = "PartitionsLost",
        Level = LogLevel.Warning,
        Message = "Partitions lost for '{IndexedKey}'. {TopicPartitionOffsets} {MemberId}")]
    public static partial void PartitionsLost(this ILogger logger, string indexedKey, string memberId,
        IEnumerable<TopicPartitionOffset> topicPartitionOffsets);

    [LoggerMessage(
        EventId = 29,
        EventName = "BufferCleanup",
        Level = LogLevel.Debug,
        Message = "Buffer cleanup for '{IndexedKey}' {Count} item(s) because {Reason}.")]
    public static partial void BufferCleanup(this ILogger logger, string indexedKey, int count, string reason);

    [LoggerMessage(
        EventId = 30,
        EventName = "ConsumeError",
        Level = LogLevel.Warning,
        Message = "Consume error for '{IndexedKey}'. {TopicPartitionOffset} {Error}")]
    public static partial void ConsumeError(this ILogger logger, Exception exception, string indexedKey,
        Error error, TopicPartitionOffset topicPartitionOffset);

    [LoggerMessage(
        EventId = 31,
        EventName = "ProducerClosing",
        Level = LogLevel.Information,
        Message = "Closing producer for '{IndexedKey}'. {Name}")]
    public static partial void ProducerClosing(this ILogger logger, string indexedKey, string name);

    [LoggerMessage(
        EventId = 32,
        EventName = "ConsumerClosing",
        Level = LogLevel.Information,
        Message = "Closing consumer for '{IndexedKey}'. {MemberId}")]
    public static partial void ConsumerClosing(this ILogger logger, string indexedKey, string memberId);

    [LoggerMessage(
        EventId = 33,
        EventName = "OffsetsCommitError",
        Level = LogLevel.Error,
        Message = "Offsets commit error for '{IndexedKey}'. {TopicPartitionOffsets}")]
    public static partial void OffsetsCommitError(this ILogger logger, Exception exception, string indexedKey,
        IEnumerable<TopicPartitionOffset> topicPartitionOffsets);

    [LoggerMessage(
        EventId = 34,
        EventName = "KafkaCommitFailed",
        Level = LogLevel.Warning,
        Message = "Offsets commit to kafka error for '{IndexedKey}' with external state. {TopicPartitionOffsets}")]
    public static partial void KafkaCommitFailed(this ILogger logger, Exception exception, string indexedKey,
        IEnumerable<TopicPartitionOffset> topicPartitionOffsets);

    [LoggerMessage(
        EventId = 35,
        EventName = "ConsumerDisposeError",
        Level = LogLevel.Warning,
        Message = "Consumer dispose error for '{IndexedKey}'.")]
    public static partial void ConsumerDisposeError(this ILogger logger, Exception exception, string indexedKey);

    [LoggerMessage(
        EventId = 36,
        EventName = "ProducerDisposeError",
        Level = LogLevel.Warning,
        Message = "Producer dispose error for '{IndexedKey}'.")]
    public static partial void ProducerDisposeError(this ILogger logger, Exception exception, string indexedKey);

    [LoggerMessage(
        EventId = 500,
        EventName = "PipelineRetry",
        Level = LogLevel.Error,
        Message =
            "Pipeline retry for '{IndexedKey}'. Iteration {Iteration}/{MaxIterations} after timeout {NextRetryTimeSpan}")]
    public static partial void PipelineRetry(this ILogger logger, Exception exception, string indexedKey, int iteration,
        int maxIterations, TimeSpan nextRetryTimeSpan);

    [LoggerMessage(
        EventId = 502,
        EventName = "PipelineFailed",
        Level = LogLevel.Critical,
        Message = "Pipeline failed for '{IndexedKey}'.")]
    public static partial void PipelineFailed(this ILogger logger, Exception exception, string indexedKey);

    [LoggerMessage(
        EventId = 501,
        EventName = "BatchRetry",
        Level = LogLevel.Error,
        Message =
            "Batch retry for '{IndexedKey}'. Iteration {Iteration}/{MaxIterations} after timeout {NextRetryTimeSpan} with batch size {BatchSize}.")]
    public static partial void BatchRetry(this ILogger logger, Exception exception, string indexedKey, int iteration,
        int maxIterations, TimeSpan nextRetryTimeSpan, int batchSize);

    [LoggerMessage(
        EventId = 90,
        EventName = "TransactionAbort",
        Level = LogLevel.Warning,
        Message = "Abort transaction for {IndexedKey}.")]
    public static partial void TransactionAbort(this ILogger logger, string indexedKey);

    [LoggerMessage(
        EventId = 91,
        EventName = "TransactionInit",
        Level = LogLevel.Information,
        Message = "Init transactions for {IndexedKey}.")]
    public static partial void TransactionInit(this ILogger logger, string indexedKey);

    [LoggerMessage(
        EventId = 92,
        EventName = "EntityConvertError",
        Level = LogLevel.Error,
        Message = "Entity of type {EntityType} convert to message error handled.")]
    public static partial void ConvertError(this ILogger logger, Exception exception, Type entityType);
}