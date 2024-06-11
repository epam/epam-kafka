// Copyright © 2024 EPAM Systems

using System.Globalization;

using Confluent.Kafka;

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
}