﻿// Copyright © 2024 EPAM Systems

using System.Diagnostics.Metrics;
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
    /// Config key to define logger category prefix for default log handler configured by <see cref="IKafkaFactory"/> implementation.
    /// </summary>
    /// <remarks>
    /// This key is not standard, so that causing errors when passed to producer or consumer builder.
    /// Default <see cref="IKafkaFactory"/> implementation use it only for logger configuration and don't pass it to producer or consumer builder to avoid errors.
    /// </remarks> 
    public const string DotnetLoggerCategoryKey = "dotnet.logger.category";

    /// <summary>
    /// Config key to define whether expose metrics based on statistics emitted by librdkafka. Metrics emitted using <see cref="Meter"/> with  name <see cref="Statistics.MeterName"/>
    /// </summary>
    /// <remarks>
    /// This key is not standard, so that causing errors when passed to producer or consumer builder.
    /// Default <see cref="IKafkaFactory"/> implementation use it only for metrics switch and don't pass it to producer or consumer builder to avoid errors.
    /// Works only if 'statistics.interval.ms' is defined and statistics handler not assigned explicitly in producer/consumer builder, otherwise silently not working.
    /// </remarks>
    public const string DotnetStatisticMetricsKey = "dotnet.statistics.metrics";

    private const string DotnetLoggerCategoryDefault = "Epam.Kafka.DefaultLogHandler";

    /// <summary>
    /// Read and return 'dotnet.logger.category' value if it exists, default value 'Epam.Kafka.DefaultLogHandler' otherwise. <inheritdoc cref="DotnetLoggerCategoryKey"/>
    /// </summary>
    /// <remarks><inheritdoc cref="DotnetLoggerCategoryKey"/></remarks>
    /// <param name="config">The config</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string GetDotnetLoggerCategory(this Config config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        string result = DotnetLoggerCategoryDefault;

        string? s = config.Where(prop => prop.Key == DotnetLoggerCategoryKey).Select(a => a.Value).FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(s))
        {
            result = s;
        }

        return result;
    }

    /// <summary>
    /// Read and return 'dotnet.statistics.metrics' value if it exists, default value <c>false</c> otherwise. <inheritdoc cref="DotnetStatisticMetricsKey"/>
    /// </summary>
    /// <remarks><inheritdoc cref="DotnetStatisticMetricsKey"/></remarks>
    /// <param name="config">The config</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static bool GetDotnetStatisticMetrics(this Config config)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        string? s = config.Where(prop => prop.Key == DotnetStatisticMetricsKey).Select(a => a.Value).FirstOrDefault();

        return string.Equals(bool.TrueString, s, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Set 'dotnet.logger.category' value to config. <inheritdoc cref="DotnetLoggerCategoryKey"/>
    /// </summary>
    /// <remarks><inheritdoc cref="DotnetLoggerCategoryKey"/></remarks>
    /// <param name="config">The config to update</param>
    /// <param name="value">The value</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static void SetDotnetLoggerCategory(this Config config, string value)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (value == null) throw new ArgumentNullException(nameof(value));

        config.Set(DotnetLoggerCategoryKey, value);
    }

    /// <summary>
    /// Set 'dotnet.statistics.metrics' value to config. <inheritdoc cref="DotnetStatisticMetricsKey"/>
    /// </summary>
    /// <remarks><inheritdoc cref="DotnetStatisticMetricsKey"/></remarks>
    /// <param name="config">The config to update</param>
    /// <param name="value">The value</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static void SetDotnetStatisticMetrics(this Config config, bool value)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        config.Set(DotnetStatisticMetricsKey, value.ToString(CultureInfo.InvariantCulture).ToLowerInvariant());
    }

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

            if (result is < DotnetCancellationDelayMaxMsMin or > DotnetCancellationDelayMaxMsMax)
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

        if (value is < DotnetCancellationDelayMaxMsMin or > DotnetCancellationDelayMaxMsMax)
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
            foreach (KeyValuePair<string, string> kvp in placeholders)
            {
                ValidatePlaceholder(kvp.Key, kvp.Value);
            }
        }

        TConfig result = new();

        foreach (KeyValuePair<string, string> x in config.Where(x => x.Value != null))
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
            foreach (KeyValuePair<string, string> kvp in placeholders)
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