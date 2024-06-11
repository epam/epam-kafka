// Copyright © 2024 EPAM Systems

using System.Text.RegularExpressions;

namespace Epam.Kafka.Internals;

internal static
#if NET8_0_OR_GREATER
partial
#endif
class RegexHelper
{
    // topic name (e.g. qwe-1)
    private const string ConfigPlaceholderRegexValue = @"^<[\d\w]{1,}>$";
    public static Regex ConfigPlaceholderRegex { get; } = GetConfigPlaceholderRegex();

#if NET8_0_OR_GREATER
    [GeneratedRegex(ConfigPlaceholderRegexValue)]
    private static partial Regex GetConfigPlaceholderRegex();
#else
    private static Regex GetConfigPlaceholderRegex()
    {
        return new(ConfigPlaceholderRegexValue, RegexOptions.Compiled);
    }
#endif
}