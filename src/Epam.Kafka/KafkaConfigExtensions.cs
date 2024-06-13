// Copyright © 2024 EPAM Systems

using System.Globalization;
using System.Text.RegularExpressions;

using Confluent.Kafka;

#if !NET6_0_OR_GREATER
using Epam.Kafka.Internals;
#else
using RegexHelper = Epam.Kafka.Internals.RegexHelper;
#endif

namespace Epam.Kafka;

/// <summary>
///     Extensions methods to work with kafka configs.
/// </summary>
public static class KafkaConfigExtensions
{
    private const string DotnetCancellationDelayMaxMsKey = "dotnet.cancellation.delay.max.ms";
    private const int DotnetCancellationDelayMaxMsDefault = 100;
    private const int DotnetCancellationDelayMaxMsMin = 1;
    private const int DotnetCancellationDelayMaxMsMax = 10000;

    /// <summary>
    /// Read and return 'dotnet.cancellation.delay.max.ms' value if it exists, default value 100 otherwise.
    /// </summary>
    /// <param name="config">The config</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static int GetCancellationDelayMaxMs(this ConsumerConfig config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        int result = DotnetCancellationDelayMaxMsDefault;

        string? s = config.Where(prop => prop.Key == DotnetCancellationDelayMaxMsKey).Select(a => a.Value).FirstOrDefault();

        if (s != null)
        {
            if (!int.TryParse(s, out result))
            {
                throw new ArgumentException($"'{DotnetCancellationDelayMaxMsKey}' must be a valid integer value.");
            }

            if (result < DotnetCancellationDelayMaxMsMin || result > DotnetCancellationDelayMaxMsMax)
            {
                throw new ArgumentOutOfRangeException(nameof(config), result, $"'{DotnetCancellationDelayMaxMsKey}' must be in the range {DotnetCancellationDelayMaxMsMin} <= '{DotnetCancellationDelayMaxMsKey}' <= {DotnetCancellationDelayMaxMsMax}");
            }
        }

        return result;
    }

    /// <summary>
    /// Set 'dotnet.cancellation.delay.max.ms' value to config.
    /// </summary>
    /// <param name="config">The config to update</param>
    /// <param name="value">The value</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static void SetCancellationDelayMaxMs(this ConsumerConfig config, int value)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        if (value < DotnetCancellationDelayMaxMsMin || value > DotnetCancellationDelayMaxMsMax)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"'{DotnetCancellationDelayMaxMsKey}' must be in the range {DotnetCancellationDelayMaxMsMin} <= '{DotnetCancellationDelayMaxMsKey}' <= {DotnetCancellationDelayMaxMsMax}");
        }

        config.Set(DotnetCancellationDelayMaxMsKey, value.ToString("D", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Clone existing config and optionally replace placeholders if <paramref name="placeholders"/> is not null
    /// </summary>
    /// <param name="config">The base config</param>
    /// <param name="placeholders">Optional placeholders to replace in clone only. base config not modified.</param>
    /// <typeparam name="TConfig">The config type</typeparam>
    /// <returns>New instance of config.</returns>
    public static TConfig Clone<TConfig>(this TConfig config, IReadOnlyDictionary<string, string>? placeholders = null)
        where TConfig : Config, new()
    {
        if (placeholders != null)
        {
            foreach (var kvp in placeholders)
            {
                ValidatePlaceholder(kvp.Key, kvp.Value);
            }
        }

        TConfig result = new();

        foreach (var x in config.Where(x => x.Value != null))
        {
            result.Set(x.Key, ReplacePlaceholdersIfNeeded(x.Value, placeholders));
        }

        return result;
    }

    internal static string ReplacePlaceholdersIfNeeded(
        string value, IReadOnlyDictionary<string, string>? placeholders)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));

        if (placeholders is { Count: > 0 })
        {
            foreach (var kvp in placeholders)
            {
                value = value.Replace(kvp.Key, kvp.Value, StringComparison.OrdinalIgnoreCase);
            }
        }

        return value;
    }

    internal static void ValidatePlaceholder(string key, string value)
    {
        Regex regex = RegexHelper.ConfigPlaceholderRegex;

        if (key == null || !regex.IsMatch(key))
        {
            throw new ArgumentException($"Placeholder key '{key}' not match '{regex}'.", nameof(key));
        }

        if (value == null || regex.IsMatch(value))
        {
            throw new ArgumentException($"Placeholder value '{value}' for key {key} is null or match '{regex}'.",
                nameof(value));
        }
    }
}