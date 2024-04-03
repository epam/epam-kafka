// Copyright © 2024 EPAM Systems

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Epam.Kafka.Options.Configuration;

internal abstract class OptionsFromConfiguration<TOptions> : IConfigureNamedOptions<TOptions>
    where TOptions : class
{
    private readonly IConfiguration _configuration;

    protected OptionsFromConfiguration(IConfiguration configuration)
    {
        this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected abstract string ParentSectionName { get; }

    public void Configure(TOptions options)
    {
        this.Configure(Microsoft.Extensions.Options.Options.DefaultName, options);
    }

    public void Configure(string? name, TOptions options)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        IConfigurationSection section = this._configuration.GetSection(this.ParentSectionName).GetSection(name);

        if (section.Exists())
        {
            Dictionary<string, string> items = BindDictionary(section);

            this.ConfigureInternal(options, items);
        }
    }

    protected abstract void ConfigureInternal(TOptions options, Dictionary<string, string> items);

    private static Dictionary<string, string> BindDictionary(IConfigurationSection section)
    {
        var result = new Dictionary<string, string>();

        foreach (IConfigurationSection? x in section.GetChildren())
        {
            if (x.Value != null)
            {
                result.Add(x.Key, x.Value);
            }
        }

        return result;
    }
}