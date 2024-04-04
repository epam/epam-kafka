// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Options;
using Epam.Kafka.PubSub.Publication;
using Epam.Kafka.PubSub.Subscription;

namespace Epam.Kafka.PubSub.Common.Pipeline;

/// <summary>
///     Batch processing state.
/// </summary>
public enum BatchStatus
{
    /// <summary>
    ///     Pipeline not running or first batch in pipeline not yet started.
    /// </summary>
    None,

    /// <summary>
    ///     <list type="string">For Subscription consuming messages from kafka topics.</list>
    ///     <list type="string">For Publication execution of <see cref="IPublicationHandler{TKey,TValue}.GetBatch" />.</list>
    /// </summary>
    Reading,

    /// <summary>
    ///     Waiting in the queue due to <see cref="PubSubOptions.HandlerConcurrencyGroup" /> settings.
    /// </summary>
    Queued,

    /// <summary>
    ///     <list type="string">For Subscription execution of <see cref="ISubscriptionHandler{TKey,TValue}.Execute" />.</list>
    ///     <list type="string">For Publication producing messages to kafka topics.</list>
    /// </summary>
    Running,

    /// <summary>
    ///     <list type="string">
    ///         For Subscription commiting offsets to internal state or using
    ///         <see cref="IExternalOffsetsStorage.CommitOrReset" />
    ///     </list>
    ///     <list type="string">
    ///         For Publication execution of <see cref="IPublicationHandler{TKey,TValue}.ReportResults" /> and
    ///         optionally <see cref="IPublicationHandler{TKey,TValue}.TransactionCommitted" />
    ///     </list>
    /// </summary>
    Commiting,

    /// <summary>
    ///     Batch execution finished.
    /// </summary>
    Finished
}