// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Microsoft.Extensions.Configuration;

namespace Epam.Kafka.Options.Configuration;

internal class ConsumerOptionsConfigure : OptionsFromConfiguration<KafkaConsumerOptions>
{
    public ConsumerOptionsConfigure(IConfiguration configuration) : base(configuration)
    {
    }

    protected override string ParentSectionName => "Kafka:Consumers";

    protected override void ConfigureInternal(KafkaConsumerOptions options, Dictionary<string, string> items)
    {
        options.ConsumerConfig = new ConsumerConfig(items);

        if (options.ConsumerConfig.GroupId != null)
        {
            // support placeholders in config
            options.ConsumerConfig.GroupId = options.ConsumerConfig.GroupId
                .Replace("<MachineName>", Environment.MachineName
#if NET6_0_OR_GREATER
                    , StringComparison.OrdinalIgnoreCase
#endif
                )
                .Replace("<UserName>", Environment.UserName
#if NET6_0_OR_GREATER
                    , StringComparison.OrdinalIgnoreCase
#endif
                );
        }
    }
}