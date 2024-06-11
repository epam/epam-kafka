// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Microsoft.Extensions.Configuration;

namespace Epam.Kafka.Options.Configuration;

internal class ConsumerOptionsConfigure : OptionsFromConfiguration<KafkaConsumerOptions>
{
    public ConsumerOptionsConfigure(IConfiguration configuration, KafkaBuilder kafkaBuilder) : base(configuration, kafkaBuilder)
    {
    }

    protected override string ParentSectionName => "Kafka:Consumers";

    protected override void ConfigureInternal(KafkaConsumerOptions options, Dictionary<string, string> items)
    {
        options.ConsumerConfig = new ConsumerConfig(items);
    }
}