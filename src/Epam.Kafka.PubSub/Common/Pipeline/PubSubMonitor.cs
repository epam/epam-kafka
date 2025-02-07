// Copyright © 2024 EPAM Systems

namespace Epam.Kafka.PubSub.Common.Pipeline;

/// <inheritdoc />
/// <typeparam name="TBatchResult">The type of batch result.</typeparam>
public abstract class PubSubMonitor<TBatchResult> : PipelineMonitor
    where TBatchResult : struct, Enum
{
    /// <inheritdoc />
    internal PubSubMonitor(PubSubContext context, string name) : base(context, name)
    {
    }

    /// <summary>
    ///     The last batch result.
    /// </summary>
    public StatusDetails<TBatchResult> Result { get; } = new(default);

    /// <inheritdoc />
    public override string ToString()
    {
        return
            $"Pipeline iteration {this.PipelineRetryIteration}: {this.Pipeline}, Batch: {this.Batch}, Result: {this.Result}";
    }

    internal abstract void HandleResult(TBatchResult batchResult);
}