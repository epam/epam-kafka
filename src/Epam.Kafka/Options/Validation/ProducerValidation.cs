// Copyright © 2024 EPAM Systems

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

        // nothing additional to validate
        return ValidateOptionsResult.Success;
    }
}