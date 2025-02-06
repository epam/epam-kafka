// Copyright © 2024 EPAM Systems

namespace Epam.Kafka.Stats.Eos;

/// <summary>
/// Enum representing the current states of a transactional producer.
/// see https://github.com/confluentinc/librdkafka/blob/master/src/rdkafka_int.h
/// </summary>
public enum TransactionalProducerState
{
    /// <summary>
    /// State not available
    /// </summary>
    None,

    /// <summary>
    /// Initial state when the producer is created, before being initialized for transactions.
    /// </summary>
    Init,

    /// <summary>
    /// Waiting for a Producer ID (PID) to be assigned by the broker.
    /// </summary>
    WaitPID,

    /// <summary>
    /// The producer is ready but is waiting for acknowledgment of ready state from the broker for recovery.
    /// </summary>
    ReadyNotAcked,

    /// <summary>
    /// Producer has a valid PID and is ready to begin transactions.
    /// </summary>
    Ready,

    /// <summary>
    /// A transaction is currently in progress.
    /// </summary>
    InTransaction,

    /// <summary>
    /// The producer is preparing to commit the ongoing transaction.
    /// </summary>
    BeginCommit,

    /// <summary>
    /// The ongoing transaction is being committed.
    /// </summary>
    CommittingTransaction,

    /// <summary>
    /// Transaction successfully committed but application has not made
    /// a successful commit_transaction() call yet.
    /// </summary>
    CommitNotAcked,

    /// <summary>
    /// begin_abort() has been called.
    /// </summary>
    BeginAbort,

    /// <summary>
    /// The ongoing transaction is being aborted.
    /// </summary>
    AbortingTransaction,

    /// <summary>
    /// Transaction successfully aborted but abort_transaction() has not been successfully called yet.
    /// </summary>
    AbortNotAcked,

    /// <summary>
    /// An abortable error occurred. The current transaction needs to be aborted, but the producer can start a new transaction after the abort.
    /// </summary>
    AbortableError,

    /// <summary>
    /// A fatal error occurred that prevents continued transactional production. 
    /// The producer can no longer be used for transactions.
    /// </summary>
    FatalError,
}