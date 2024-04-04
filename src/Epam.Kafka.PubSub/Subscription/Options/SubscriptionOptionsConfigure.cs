// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Options;

using Microsoft.Extensions.Configuration;

namespace Epam.Kafka.PubSub.Subscription.Options;

internal class SubscriptionOptionsConfigure : PubSubOptionsConfigure<SubscriptionOptions>
{
    public SubscriptionOptionsConfigure(IConfiguration configuration) : base(configuration)
    {
    }

    protected override string DefaultSectionName => "Kafka:Default:Subscription";
    protected override string SectionNamePrefix => "Kafka:Subscriptions";

    protected override void Bind(IConfigurationSection section, SubscriptionOptions options)
    {
        section.Bind(options, x =>
        {
            x.ErrorOnUnknownConfiguration = true;
            x.BindNonPublicProperties = false;
        });
    }
}