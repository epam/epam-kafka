// Copyright © 2024 EPAM Systems

using Epam.Kafka.Internals;
using System.Text.RegularExpressions;

namespace Epam.Kafka;

public partial class KafkaBuilder
{
    private readonly Dictionary<string, string> _configPlaceholders = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// <inheritdoc cref="WithConfigPlaceholders"/><inheritdoc cref="WithConfigPlaceholders" path="/param[@name='placeholders']"/>. For modification use <see cref="WithConfigPlaceholders"/>
    /// </summary>
    internal IReadOnlyDictionary<string, string> ConfigPlaceholders => this._configPlaceholders;

    /// <summary>
    /// Configure placeholders that can be used in 'Kafka:Consumers', 'Kafka:Producers', and 'Kafka:Clusters' configuration sections.
    /// </summary>
    /// <param name="placeholders">Mapping between CASE INSENSITIVE key and value. Key must match '^&lt;[\d\w]{1,}&gt;$' regex, value must not match it.</param>
    /// <returns>The <see cref="KafkaBuilder" /></returns>
    /// <remarks>Placeholders work only for values that were read from corresponding <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> when kafka builder added with 'useConfiguration = true' parameter.</remarks>
    /// <exception cref="InvalidOperationException">In case of kafka builder added with useConfiguration = false parameter.</exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public KafkaBuilder WithConfigPlaceholders(IEnumerable<KeyValuePair<string, string>> placeholders)
    {
        if (placeholders == null) throw new ArgumentNullException(nameof(placeholders));

        if (!this._useConfiguration)
        {
            throw new InvalidOperationException(
                "Config placeholders can be used only for builder created with 'useConfiguration = true' parameter.");
        }

        if (this.ConfigPlaceholders.Count > 0)
        {
            throw new InvalidOperationException("Config placeholders already set.");
        }

        Regex regex = RegexHelper.ConfigPlaceholderRegex;

        foreach (KeyValuePair<string, string> pair in placeholders)
        {
            if (pair.Key == null || !regex.IsMatch(pair.Key))
            {
                throw new ArgumentException($"Placeholder key '{pair.Key}' not match {regex}.", nameof(placeholders));
            }

            if (pair.Value == null || regex.IsMatch(pair.Value))
            {
                throw new ArgumentException($"Placeholder value '{pair.Value}' is null or match {regex}.", nameof(placeholders));
            }

            try
            {
                this._configPlaceholders.Add(pair.Key, pair.Value);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException($"Duplicate CASE INSENSITIVE key '{pair.Key},.", nameof(placeholders), e);
            }
        }

        return this;
    }
}