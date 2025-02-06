// Copyright © 2024 EPAM Systems

using System.Text.Json.Serialization;

namespace Epam.Kafka.Stats.Eos;

/// <summary>
/// EOS / Idempotent producer state and metrics
/// </summary>
public class TransactionStatistics
{
    /// <summary>
    /// Current idempotent producer id state.
    /// </summary>
    [JsonPropertyName("idemp_state")]
    public IdempotentProducerIdState IdempotentState { get; set; } = IdempotentProducerIdState.None;

    /// <summary>
    /// Time elapsed since last <see cref="IdempotentState"/> change (milliseconds).
    /// </summary>
    [JsonPropertyName("idemp_stateage")]
    public long IdempotentAgeMilliseconds { get; set; }

    /// <summary>
    /// Current transactional producer state.
    /// </summary>
    [JsonPropertyName("txn_state")]
    public TransactionalProducerState TransactionState { get; set; } = TransactionalProducerState.None;

    /// <summary>
    /// Time elapsed since last <see cref="TransactionState"/> change (milliseconds).
    /// </summary>
    [JsonPropertyName("txn_stateage")]
    public long TransactionAgeMilliseconds { get; set; }

    /// <summary>
    /// Transactional state allows enqueuing (producing) new messages.
    /// </summary>
    [JsonPropertyName("txn_may_enq")]
    public bool EnqAllowed { get; set; }
}