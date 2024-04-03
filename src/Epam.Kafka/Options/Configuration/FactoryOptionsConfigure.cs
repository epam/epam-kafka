// Copyright © 2024 EPAM Systems

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Epam.Kafka.Options.Configuration;

internal class FactoryOptionsConfigure : IConfigureOptions<KafkaFactoryOptions>
{
    private const string KafkaDefaultSection = "Kafka:Default";

    private readonly IConfiguration _configuration;

    public FactoryOptionsConfigure(IConfiguration configuration)
    {
        this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public void Configure(KafkaFactoryOptions options)
    {
        IConfigurationSection section = this._configuration.GetSection(KafkaDefaultSection);

        options.Cluster = section[nameof(KafkaFactoryOptions.Cluster)];
        options.Consumer = section[nameof(KafkaFactoryOptions.Consumer)];
        options.Producer = section[nameof(KafkaFactoryOptions.Producer)];
    }
}