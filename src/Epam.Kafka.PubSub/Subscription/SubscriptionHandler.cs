// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Utils;

using Microsoft.Extensions.Logging;

using System.Diagnostics;

namespace Epam.Kafka.PubSub.Subscription;

/// <summary>
///     Base class to implement <see cref="ISubscriptionHandler{TKey,TValue}" /> that allow client side message compaction
///     and filtering.
/// </summary>
/// <typeparam name="TKey">The message key type.</typeparam>
/// <typeparam name="TValue">The message value type.</typeparam>
public abstract class SubscriptionHandler<TKey, TValue> : ISubscriptionHandler<TKey, TValue>
    where TKey : notnull
{
    private const string CompactedResult = "Compacted";
    private const string FilteredOutResult = "FilteredOut";
    private const string UnknownResult = "Unknown";

    /// <summary>
    ///     Initialize the <see cref="SubscriptionHandler{TKey,TValue}" /> instance.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger" />.</param>
    /// <exception cref="ArgumentNullException"></exception>
    protected SubscriptionHandler(ILogger logger)
    {
        this.Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Whether client side compaction allowed. Default <c>true</c>.
    ///     <remarks> If <c>true</c> </remarks>
    ///     and multiple messages with same key appeared in the batch,
    ///     then only last message for such kind of key will be passed to <see cref="ProcessBatch" /> for further processing.
    /// </summary>
    protected bool AllowCompaction { get; set; } = true;

    /// <summary>
    ///     Predicate for client side message filtering. Default <c>null</c> means that filtering disabled.
    ///     <remarks>
    ///         If not <c>null</c> predicate will be invoked to check whether message should be passed to
    ///         <see cref="ProcessBatch" /> for further processing.
    ///     </remarks>
    /// </summary>
    protected Func<ConsumeResult<TKey, TValue>, bool>? Filter { get; set; }

    /// <summary>
    ///     The <see cref="ILogger" />.
    /// </summary>
    protected ILogger Logger { get; set; }

    /// <inheritdoc />
    public void Execute(IReadOnlyCollection<ConsumeResult<TKey, TValue>> items, CancellationToken cancellationToken)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        Dictionary<ConsumeResult<TKey, TValue>, string?> results = this.TryIgnore(items);

        cancellationToken.ThrowIfCancellationRequested();

        this.TryCompact(results);

        cancellationToken.ThrowIfCancellationRequested();

        int count = results.Count(x => x.Value == null);

        if (count > 0)
        {
            this.ProcessBatch(results, cancellationToken);
        }

        this.Logger.BatchHandlerExecuted(items.Count,
            results.GroupBy(x => x.Value ?? UnknownResult)
                .Select(g => new KeyValuePair<string, int>(g.Key, g.Count())));
    }

    /// <summary>
    ///     Invoked to iterate through messages that have not been compacted or filtered out and invoke
    ///     <see cref="ProcessSingle" /> method for them.
    /// </summary>
    /// <param name="items">The items to process after optional client side compaction and filtering.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken" />.</param>
    /// <exception cref="ArgumentNullException"></exception>
    protected virtual void ProcessBatch(IDictionary<ConsumeResult<TKey, TValue>, string?> items,
        CancellationToken cancellationToken)
    {
        if (items == null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        foreach (KeyValuePair<ConsumeResult<TKey, TValue>, string?> item in items.Where(x => x.Value == null))
        {
            cancellationToken.ThrowIfCancellationRequested();
            items[item.Key] = this.ProcessSingle(item.Key);
        }
    }

    /// <summary>
    ///     Invoked for each message by default <see cref="ProcessBatch" /> implementation.
    ///     <remarks>
    ///         By default throw <see cref="NotImplementedException" />.
    ///         MUST be overridden if default implementation of <see cref="ProcessBatch" /> is used.
    ///     </remarks>
    /// </summary>
    /// <param name="item">The items to process after optional client side compaction and filtering.</param>
    /// <returns>
    ///     Free text message processing status. Will be used only for logging. If <c>null</c> returned then status
    ///     'Unknown' will be used.
    /// </returns>
    /// <exception cref="NotImplementedException">
    ///     Default behaviour, method must be overridden if default implementation of
    ///     <see cref="ProcessBatch" /> is used.
    /// </exception>
    protected virtual string ProcessSingle(ConsumeResult<TKey, TValue> item)
    {
        throw new NotImplementedException();
    }

    private Dictionary<ConsumeResult<TKey, TValue>, string?> TryIgnore(
        IReadOnlyCollection<ConsumeResult<TKey, TValue>> items)
    {
        var result = new Dictionary<ConsumeResult<TKey, TValue>, string?>(items.Count);

        Func<ConsumeResult<TKey, TValue>, bool>? filter = this.Filter;
        Activity? activity = null;

        if (filter != null)
        {
            activity = new Activity("Filter");
            activity.Start();
        }

        try
        {
            foreach (ConsumeResult<TKey, TValue> item in items)
            {
                if (filter != null && !filter.Invoke(item))
                {
                    result.Add(item, FilteredOutResult);
                }
                else
                {
                    result.Add(item, null);
                }
            }

            return result;
        }
        finally
        {
            activity?.Stop();
            activity?.Dispose();
        }
    }

    private void TryCompact(Dictionary<ConsumeResult<TKey, TValue>, string?> results)
    {
        if (this.AllowCompaction)
        {
            var compaction = new Dictionary<TKey, Dictionary<TopicPartition, ConsumeResult<TKey, TValue>>>();

            foreach (KeyValuePair<ConsumeResult<TKey, TValue>, string?> item in results.Where(x => x.Value == null))
            {
                if (!compaction.TryGetValue(item.Key.Message.Key,
                        out Dictionary<TopicPartition, ConsumeResult<TKey, TValue>>? offsets))
                {
                    offsets = new();
                    compaction[item.Key.Message.Key] = offsets;
                }

                if (!offsets.TryGetValue(item.Key.TopicPartition, out ConsumeResult<TKey, TValue>? existing))
                {
                    offsets[item.Key.TopicPartition] = item.Key;
                }
                else if (existing.Offset < item.Key.Offset)
                {
                    results[existing] = CompactedResult;
                }
                else
                {
                    results[item.Key] = CompactedResult;
                }
            }
        }
    }
}