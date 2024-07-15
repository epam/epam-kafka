// Copyright © 2024 EPAM Systems

using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Epam.Kafka.PubSub.Common.Options;

internal static class PubSubOptionsValidate
{
    public static string? GetFirstFailure(PubSubOptions options)
    {
        string? result = null;

        result ??= options.ValidateRange(x => x.HandlerTimeout, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(60));

        result ??= options.ValidateRange(x => x.HealthChecksThresholdPipeline, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(60));
        result ??= options.ValidateRange(x => x.HealthChecksThresholdBatch, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(60));

        result ??= options.ValidateRange(x => x.BatchRetryMaxTimeout, TimeSpan.Zero, TimeSpan.FromMinutes(10));
        result ??= options.ValidateRange(x => x.BatchEmptyTimeout, TimeSpan.Zero, TimeSpan.FromMinutes(10));
        result ??= options.ValidateRange(x => x.BatchRetryCount, 0, 20);

        result ??= options.ValidateRange(x => x.PipelineRetryTimeout, TimeSpan.Zero, TimeSpan.FromMinutes(60));
        result ??= options.ValidateRange(x => x.PipelineRetryCount, 0, int.MaxValue);

        result ??= options.ValidateRange(x => x.BatchSize, 0, int.MaxValue);

        return result;
    }

    public static string? ValidateRange<TOptions, TValue>(this TOptions options,
        Expression<Func<TOptions, TValue>> member, TValue min, TValue max)
        where TOptions : PubSubOptions
        where TValue : IComparable<TValue>
    {
        string? name = (member.Body as MemberExpression)?.Member.Name;

        TValue value = member.Compile().Invoke(options);

        if (value.CompareTo(min) < 0)
        {
            return $"{name} less than '{min}'.";
        }

        if (value.CompareTo(max) > 0)
        {
            return $"{name} greater than '{max}'.";
        }

        return null;
    }

    public static string? ValidateString<TOptions>(this TOptions options, Expression<Func<TOptions, string?>> member,
        bool canBeNullOrWhitespace = false, Regex? regex = null)
        where TOptions : PubSubOptions
    {
        string? name = (member.Body as MemberExpression)?.Member.Name;

        string? value = member.Compile().Invoke(options);

        if (!canBeNullOrWhitespace && string.IsNullOrWhiteSpace(value))
        {
            return $"{name} is null or empty.";
        }

        if (value != null && regex != null && !regex.IsMatch(value))
        {
            return $"{name} is not match '{regex}'.";
        }

        return null;
    }
}