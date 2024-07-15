// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Pipeline;

namespace Epam.Kafka.PubSub.Publication.Pipeline;

/// <summary>
///     The result of publication batch iteration.
/// </summary>
public enum PublicationBatchResult
{
    /// <summary>
    ///     Result not available. Might be the case for first batch after pipeline (re)start.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Finished with errors.
    /// </summary>
    Error = BatchResult.Error,

    /// <summary>
    ///     Finished without messages to publish.
    /// </summary>
    Empty = BatchResult.Empty,

    /// <summary>
    ///     All messages were successfully published.
    /// </summary>
    Processed = BatchResult.Processed,

    /// <summary>
    ///     At least one message was successfully published and at least one message was not successfully published.
    ///     In case of transactional producer transaction will be rolled back, so successfully published message will not be
    ///     available in kafka.
    /// </summary>
    ProcessedPartial = 30,
}