// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Microsoft.Extensions.Configuration;

namespace Epam.Kafka.Options.Configuration;

internal class ProducerOptionsConfigure : OptionsFromConfiguration<KafkaProducerOptions>
{
    public ProducerOptionsConfigure(IConfiguration configuration, KafkaBuilder kafkaBuilder) : base(configuration, kafkaBuilder)
    {
    }

    protected override string ParentSectionName => "Kafka:Producers";

    protected override void ConfigureInternal(KafkaProducerOptions options, Dictionary<string, string> items)
    {
        options.ProducerConfig = new ProducerConfig(items);
    }
}