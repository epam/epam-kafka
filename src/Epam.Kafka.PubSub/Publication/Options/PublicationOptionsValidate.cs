// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Options;
using Epam.Kafka.PubSub.Utils;

using Microsoft.Extensions.Options;

namespace Epam.Kafka.PubSub.Publication.Options;

internal class PublicationOptionsValidate : IValidateOptions<PublicationOptions>
{
    public ValidateOptionsResult Validate(string? name, PublicationOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        string? result = PubSubOptionsValidate.GetFirstFailure(options);

        result ??= options.ValidateString(x => x.DefaultTopic, regex: RegexHelper.TopicNameRegex);

        result ??= ValidateSerializers(options);

        if (result != null)
        {
            return ValidateOptionsResult.Fail(result);
        }

        return ValidateOptionsResult.Success;
    }

    private static string? ValidateSerializers(PublicationOptions options)
    {
        if (options.KeyType != null && options.KeySerializer == null &&
            !SerializationHelper.DefaultSerializers.TryGetValue(options.KeyType, out _))
        {
            return $"Custom serializer not set for non default key type {options.KeyType}.";
        }

        if (options.ValueType != null && options.ValueSerializer == null &&
            !SerializationHelper.DefaultSerializers.TryGetValue(options.ValueType, out _))
        {
            return $"Custom serializer not set for non default value type {options.ValueType}.";
        }

        return null;
    }
}