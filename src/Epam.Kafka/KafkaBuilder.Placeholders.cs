﻿// Copyright © 2024 EPAM Systems

using Epam.Kafka.Internals;

using System.Text.RegularExpressions;

namespace Epam.Kafka;

public partial class KafkaBuilder
{
    private readonly Dictionary<string, string> _configPlaceholders = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// <inheritdoc cref = "WithConfigPlaceholders" />. For modification use <see cref="WithConfigPlaceholders"/>.
    /// </summary>
    /// <remarks><inheritdoc cref = "WithConfigPlaceholders" /></remarks>
    internal IReadOnlyDictionary<string, string> ConfigPlaceholders => this._configPlaceholders;

    /// <summary>
    /// Configure CASE INSENSITIVE placeholders that can be used in 'Kafka:Consumers', 'Kafka:Producers', and 'Kafka:Clusters' configuration sections.
    /// </summary>
    /// <returns>The <see cref="KafkaBuilder" /></returns>
    /// <remarks>Placeholders work only for values that were read from corresponding <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> when kafka builder added with 'useConfiguration = true' parameter.
    /// Default placeholders that added automatically:
    /// <list type="string">.WithConfigPlaceholders("&lt;DomainName&gt;", AppDomain.CurrentDomain.FriendlyName)</list>
    /// <list type="string">.WithConfigPlaceholders("&lt;MachineName&gt;", Environment.MachineName)</list>
    /// </remarks>
    /// <exception cref="InvalidOperationException">In case of kafka builder added with useConfiguration = false parameter.</exception>
    /// <param name="key">Key represent CASE INSENSITIVE text token to replace, and must match '^&lt;[\d\w]{1,}&gt;$' regex.</param>
    /// <param name="value">Value represent replacement value, and must NOT match '^&lt;[\d\w]{1,}&gt;$' regex.</param>
    public KafkaBuilder WithConfigPlaceholders(string key, string value)
    {
        if (!this._useConfiguration)
        {
            throw new InvalidOperationException(
                "Config placeholders can be used only for builder created with 'useConfiguration = true' parameter.");
        }

        Regex regex = RegexHelper.ConfigPlaceholderRegex;

        if (key == null || !regex.IsMatch(key))
        {
            throw new ArgumentException($"Placeholder key '{key}' not match '{regex}'.", nameof(key));
        }

        if (value == null || regex.IsMatch(value))
        {
            throw new ArgumentException($"Placeholder value '{value}' is null or match '{regex}'.", nameof(value));
        }

        try
        {
            this._configPlaceholders.Add(key, value);
        }
        catch (ArgumentException e)
        {
            if (!string.Equals(value, this._configPlaceholders[key], StringComparison.Ordinal))
            {
                throw new ArgumentException($"Duplicate CASE INSENSITIVE key '{key}' with value that not equal to existing.", nameof(key), e);
            }
        }

        return this;
    }
}