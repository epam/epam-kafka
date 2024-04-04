// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Options;

namespace Epam.Kafka.PubSub.Common.Pipeline;

/// <summary>
///     Pipeline processing state.
/// </summary>
public enum PipelineStatus
{
    /// <summary>
    ///     Pipeline not started.
    /// </summary>
    None,

    /// <summary>
    ///     Pipeline was cancelled due to application shutdown.
    /// </summary>
    Cancelled,

    /// <summary>
    ///     Pipeline disabled by <see cref="PubSubOptions.Enabled" /> option.
    /// </summary>
    Disabled,

    /// <summary>
    ///     Pipeline running.
    /// </summary>
    Running,

    /// <summary>
    ///     Pipeline not running. Will be retried after timeout.
    /// </summary>
    RetryTimeout,

    /// <summary>
    ///     Pipeline not running. Only application restart could help.
    /// </summary>
    Failed,
}