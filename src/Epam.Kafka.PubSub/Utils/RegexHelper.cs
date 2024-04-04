// Copyright © 2024 EPAM Systems

using System.Text.RegularExpressions;

namespace Epam.Kafka.PubSub.Utils;

internal static
#if NET8_0_OR_GREATER
    partial
#endif
    class RegexHelper
{
    // topic name (e.g. qwe-1)
    private const string TopicNameRegexValue = @"^[\w|\d|\.|\-]*$";
    public static Regex TopicNameRegex { get; } = GetTopicNameRegex();

    // topic name with partitions (e.g. qwe-1 [0,1])
    private const string TopicPartitionsRegexValue = @"^([\w|\d|\.|\-]*)\s*\[([\d]+[\d,]*)\]$";
    public static Regex TopicPartitionsRegex { get; } = GetTopicPartitionsRegex();

    private const string NameRegexValue = "^[a-zA-Z]{1}[a-zA-Z0-9]+$";
    public static Regex PunSubNameRegex { get; } = GetRegex();

#if NET8_0_OR_GREATER
    [GeneratedRegex(TopicPartitionsRegexValue)]
    private static partial Regex GetTopicPartitionsRegex();
#else
    private static Regex GetTopicPartitionsRegex()
    {
        return new(TopicPartitionsRegexValue, RegexOptions.Compiled);
    }
#endif

#if NET8_0_OR_GREATER
    [GeneratedRegex(TopicNameRegexValue)]
    private static partial Regex GetTopicNameRegex();
#else
    private static Regex GetTopicNameRegex()
    {
        return new(TopicNameRegexValue, RegexOptions.Compiled);
    }
#endif

#if NET8_0_OR_GREATER
    [GeneratedRegex(NameRegexValue)]
    private static partial Regex GetRegex();
#else
    private static Regex GetRegex()
    {
        return new(NameRegexValue, RegexOptions.Compiled);
    }
#endif
}