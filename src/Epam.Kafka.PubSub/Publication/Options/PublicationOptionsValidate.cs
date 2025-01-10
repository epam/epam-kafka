// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Options;
using Epam.Kafka.PubSub.Publication.Topics;
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

        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        string? result = PubSubOptionsValidate.GetFirstFailure(options);

        result ??= ValidateInternal(options);

        if (result != null)
        {
            return ValidateOptionsResult.Fail($"Publication '{name}' configuration not valid: {result}");
        }

        return ValidateOptionsResult.Success;
    }

    public static string? ValidateInternal(IPublicationTopicWrapperOptions options)
    {
        string? result = PubSubOptionsValidate.ValidateString(nameof(PublicationOptions.DefaultTopic), options.GetDefaultTopic(), regex: RegexHelper.TopicNameRegex);

        if (result != null)
        {
            return result;
        }

        Type? keyType = options.GetKeyType();

        if (keyType != null && options.GetKeySerializer() == null &&
            !SerializationHelper.DefaultSerializers.TryGetValue(keyType, out _))
        {
            return $"Custom serializer not set for non default key type {keyType}.";
        }

        Type? valueType = options.GetValueType();

        if (valueType != null && options.GetValueSerializer() == null &&
            !SerializationHelper.DefaultSerializers.TryGetValue(valueType, out _))
        {
            return $"Custom serializer not set for non default value type {valueType}.";
        }

        return null;
    }
}