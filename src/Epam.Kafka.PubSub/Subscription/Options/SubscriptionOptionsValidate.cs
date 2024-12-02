// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Options;
using Epam.Kafka.PubSub.Subscription.State;
using Epam.Kafka.PubSub.Utils;

using Microsoft.Extensions.Options;

namespace Epam.Kafka.PubSub.Subscription.Options;

internal class SubscriptionOptionsValidate : IValidateOptions<SubscriptionOptions>
{
    public ValidateOptionsResult Validate(string? name, SubscriptionOptions options)
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

        result ??= options.ValidateRange(x => x.BatchPausedTimeout, TimeSpan.Zero, TimeSpan.FromMinutes(10));
        result ??= options.ValidateRange(x => x.BatchNotAssignedTimeout, TimeSpan.Zero, TimeSpan.FromMinutes(10));

        result ??= ValidateSerializers(options);

        if (result != null)
        {
            return ValidateOptionsResult.Fail($"Subscription '{name}' configuration not valid: {result}");
        }

        return ValidateOptionsResult.Success;
    }

    private static string? ValidateSerializers(SubscriptionOptions options)
    {
        if (options.KeyType != null && options.KeyDeserializer == null &&
            !SerializationHelper.DefaultDeserializers.TryGetValue(options.KeyType, out _))
        {
            return $"Custom deserializer not set for non default key type {options.KeyType}.";
        }

        if (options.ValueType != null && options.ValueDeserializer == null &&
            !SerializationHelper.DefaultDeserializers.TryGetValue(options.ValueType, out _))
        {
            return $"Custom deserializer not set for non default value type {options.ValueType}.";
        }

        try
        {
            Type type = options.StateType;

            if (type == typeof(InternalKafkaState) ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(CombinedState<>)))
            {
                options.GetTopicNames();
            }
            else
            {
                options.GetTopicPartitions();
            }
        }
        catch (ArgumentException e)
        {
            return e.Message;
        }

        return null;
    }
}