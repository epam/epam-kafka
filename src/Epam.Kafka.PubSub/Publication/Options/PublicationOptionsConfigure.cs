// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Options;

using Microsoft.Extensions.Configuration;

namespace Epam.Kafka.PubSub.Publication.Options;

internal class PublicationOptionsConfigure : PubSubOptionsConfigure<PublicationOptions>
{
    public PublicationOptionsConfigure(IConfiguration configuration) : base(configuration)
    {
    }

    protected override string DefaultSectionName => "Kafka:Default:Publication";
    protected override string SectionNamePrefix => "Kafka:Publications";

    protected override void Bind(IConfigurationSection section, PublicationOptions options)
    {
        section.Bind(options, x =>
        {
            x.ErrorOnUnknownConfiguration = true;
            x.BindNonPublicProperties = false;
        });
    }
}