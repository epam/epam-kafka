// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Subscription.State;
using Epam.Kafka.PubSub.Utils;

using System.Text.RegularExpressions;

namespace Epam.Kafka.PubSub.Subscription.Options;

/// <summary>
///     Extensions methods to parse and assign <see cref="SubscriptionOptions.Topics" />
/// </summary>
public static class SubscriptionOptionsExtensions
{
    private static readonly char[] TopicsSeparator = { ';' };
    private static readonly char[] PartitionsSeparator = { ',' };

    internal static bool IsTopicNameWithPartition(this SubscriptionOptions options, out Type? storageType)
    {
        storageType = null;
        Type type = options.StateType;

        bool result = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ExternalState<>);

        if (result)
        {
            storageType = type.GenericTypeArguments.Single();
        }

        return result;
    }

    /// <summary>
    ///     Parse and validate <see cref="SubscriptionOptions.Topics" /> and return collection of topic names.
    /// </summary>
    /// <param name="options">The <see cref="SubscriptionOptions" />.</param>
    /// <returns>Collection of topic names</returns>
    /// <exception cref="ArgumentException"></exception>
    public static IReadOnlyCollection<string> GetTopicNames(this SubscriptionOptions options)
    {
        HashSet<string> result = new();

        foreach (Match match in options.SplitTopics(RegexHelper.TopicNameRegex))
        {
            string value = match.Groups[0].Value;

            if (!result.Add(value))
            {
                throw new ArgumentException($"Duplicate topic value '{value}'.", nameof(options));
            }
        }

        return result;
    }

    /// <summary>
    ///     Parse and validate <see cref="SubscriptionOptions.Topics" /> and return collection of topic partitions.
    /// </summary>
    /// <param name="options">The <see cref="SubscriptionOptions" />.</param>
    /// <returns>Collection of topic partitions</returns>
    /// <exception cref="ArgumentException"></exception>
    public static IReadOnlyCollection<TopicPartition> GetTopicPartitions(this SubscriptionOptions options)
    {
        HashSet<TopicPartition> result = new();

        foreach (Match match in options.SplitTopics(RegexHelper.TopicPartitionsRegex))
        {
            string value = match.Groups[1].Value;

            foreach (int partition in match.Groups[2].Value
                         .Split(PartitionsSeparator, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse))
            {
                var topicPartition = new TopicPartition(value, partition);

                if (!result.Add(topicPartition))
                {
                    throw new ArgumentException($"Duplicate topic partition value '{topicPartition}'.",
                        nameof(options));
                }
            }
        }

        return result;
    }

    /// <summary>
    ///     Set value <see cref="SubscriptionOptions.Topics" /> from <paramref name="topicPartitions" />
    /// </summary>
    /// <param name="options">The <see cref="SubscriptionOptions" />.</param>
    /// <param name="topicPartitions">The list of <see cref="TopicPartition" /> to assign.</param>
    /// <returns>The options <paramref name="options" />.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static SubscriptionOptions WithTopicPartitions(this SubscriptionOptions options,
        params TopicPartition[] topicPartitions)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (topicPartitions == null) throw new ArgumentNullException(nameof(topicPartitions));

        if (topicPartitions.Length == 0)
        {
            throw new ArgumentException("Topic partitions count is 0.", nameof(topicPartitions));
        }

        options.Topics = string.Join(";", topicPartitions.Select(x => $"{x.Topic} [{x.Partition.Value}]"));

        return options;
    }

    private static IEnumerable<Match> SplitTopics(this SubscriptionOptions options, Regex regex)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (options.Topics == null) throw new ArgumentException($"{nameof(options.Topics)} is null", nameof(options));

        string[] split = options.Topics.Split(TopicsSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (string item in split)
        {
            Match match = regex.Match(item.Trim());

            if (match.Success)
            {
                yield return match;
            }
            else
            {
                throw new ArgumentException($"Topic value '{item}' not match '{regex}'.");
            }
        }
    }
}