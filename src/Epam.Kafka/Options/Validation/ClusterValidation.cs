// Copyright © 2024 EPAM Systems

using Microsoft.Extensions.Options;

namespace Epam.Kafka.Options.Validation;

internal class ClusterValidation : IValidateOptions<KafkaClusterOptions>
{
    public ValidateOptionsResult Validate(string? name, KafkaClusterOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (name == null)
        {
            return ValidateOptionsResult.Fail("Unable to create cluster without name");
        }

        if (string.IsNullOrWhiteSpace(options.ClientConfig.BootstrapServers))
        {
            return ValidateOptionsResult.Fail("bootstrap.servers is null or whitespace.");
        }

        return ValidateOptionsResult.Success;
    }
}