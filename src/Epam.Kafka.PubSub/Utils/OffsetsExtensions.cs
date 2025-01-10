// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

namespace Epam.Kafka.PubSub.Utils;

internal static class OffsetsExtensions
{
    public static void GetOffsetsRange<TKey, TValue>(this IEnumerable<ConsumeResult<TKey, TValue>> results,
        out IDictionary<TopicPartition, Offset> from, out IDictionary<TopicPartition, Offset> to)
    {
        if (results == null) throw new ArgumentNullException(nameof(results));

        from = new Dictionary<TopicPartition, Offset>();
        to = new Dictionary<TopicPartition, Offset>();

        foreach (ConsumeResult<TKey, TValue> item in results)
        {
            if (!to.TryGetValue(item.TopicPartition, out Offset currentTo))
            {
                to.Add(item.TopicPartition, item.Offset);
            }

            if (!from.TryGetValue(item.TopicPartition, out Offset currentFrom))
            {
                from.Add(item.TopicPartition, item.Offset);
            }

            if (item.Offset.Value > currentTo)
            {
                to[item.TopicPartition] = item.Offset;
            }

            if (item.Offset.Value < currentFrom)
            {
                from[item.TopicPartition] = item.Offset;
            }
        }
    }

    public static IReadOnlyCollection<TopicPartitionOffset> PrepareOffsetsToCommit(this IDictionary<TopicPartition, Offset> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));

        return items.Select(x => new TopicPartitionOffset(x.Key, x.Value + 1)).ToList();
    }
}