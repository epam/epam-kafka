// Copyright © 2024 EPAM Systems

namespace Epam.Kafka.Stats;

/// <summary>
/// Enum representing the states of an idempotent producer's ID.
/// </summary>
public enum IdempotentProducerIdState
{
    /// <summary>
    /// State not available
    /// </summary>
    None,

    /// <summary>
    /// Initial state.
    /// </summary>
    Init,

    /// <summary>
    /// Instance is terminating.
    /// </summary>
    Terminate,

    /// <summary>
    /// A fatal error has been raised.
    /// </summary>
    FatalError,

    /// <summary>
    /// Request new PID (Producer ID).
    /// </summary>
    RequestPID,

    /// <summary>
    /// Waiting for coordinator to become available.
    /// </summary>
    WaitTransport,

    /// <summary>
    /// PID requested, waiting for reply.
    /// </summary>
    WaitPID,

    /// <summary>
    /// New PID assigned.
    /// </summary>
    Assigned,

    /// <summary>
    /// Wait for outstanding ProduceRequests to finish before resetting and re-requesting a new PID.
    /// </summary>
    DrainReset,

    /// <summary>
    /// Wait for outstanding ProduceRequests to finish before bumping the current epoch.
    /// </summary>
    DrainBump,

    /// <summary>
    /// Wait for transaction abort to finish and trigger a drain and reset or bump.
    /// </summary>
    WaitTxnAbort
}