// Copyright © 2024 EPAM Systems

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Epam.Kafka.PubSub.Common.Options;

internal abstract class PubSubOptionsConfigure<TOptions> : IConfigureNamedOptions<TOptions>
    where TOptions : PubSubOptions
{
    private readonly IConfiguration _configuration;

    protected PubSubOptionsConfigure(IConfiguration configuration)
    {
        this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected abstract string DefaultSectionName { get; }
    protected abstract string SectionNamePrefix { get; }

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

        IConfigurationSection defaultSection = this._configuration.GetSection(this.DefaultSectionName);
        IConfigurationSection section = this._configuration.GetSection($"{this.SectionNamePrefix}:{name}");

        if (defaultSection.Exists())
        {
            this.Bind(defaultSection, options);
        }

        if (section.Exists())
        {
            this.Bind(section, options);
        }
    }

    protected abstract void Bind(IConfigurationSection section, TOptions options);
}