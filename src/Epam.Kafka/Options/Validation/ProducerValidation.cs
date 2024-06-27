// Copyright © 2024 EPAM Systems

using Epam.Kafka.Internals;

using Microsoft.Extensions.Options;

namespace Epam.Kafka.Options.Validation;

internal class ProducerValidation : IValidateOptions<KafkaProducerOptions>
{
    public ValidateOptionsResult Validate(string? name, KafkaProducerOptions options)
    {
        if (name == null)
        {
            return ValidateOptionsResult.Fail("Unable to create producer without name");
        }

        if (string.Equals(name, SharedClient.ProducerName, StringComparison.OrdinalIgnoreCase))
        {
            if (options.ProducerConfig.TransactionalId != null)
            {
                return ValidateOptionsResult.Fail(
                    $"Producer config with predefined logical name '{SharedClient.ProducerName}' should not use 'transactional.id'.");
            }
        }

        // nothing additional to validate
        return ValidateOptionsResult.Success;
    }
}