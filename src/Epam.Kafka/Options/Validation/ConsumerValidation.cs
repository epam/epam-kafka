// Copyright © 2024 EPAM Systems

using Microsoft.Extensions.Options;

namespace Epam.Kafka.Options.Validation;

internal class ConsumerValidation : IValidateOptions<KafkaConsumerOptions>
{
    public ValidateOptionsResult Validate(string? name, KafkaConsumerOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (name == null)
        {
            return ValidateOptionsResult.Fail("Unable to create consumer without name");
        }

        if (string.IsNullOrWhiteSpace(options.ConsumerConfig.GroupId))
        {
            return ValidateOptionsResult.Fail("group.id is null or whitespace.");
        }

        try
        {
            options.ConsumerConfig.GetCancellationDelayMaxMs();
        }
        catch (ArgumentException e)
        {
            return ValidateOptionsResult.Fail(e.Message);
        }

        return ValidateOptionsResult.Success;
    }
}