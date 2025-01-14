// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Options;
using Epam.Kafka.PubSub.Publication.Options;
using Epam.Kafka.PubSub.Utils;

using Microsoft.Extensions.Options;

using ReplicationOptions = Epam.Kafka.PubSub.Subscription.Replication.ReplicationOptions;

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

        result ??= ValidateReplication(options.Replication);

        if (result != null)
        {
            return ValidateOptionsResult.Fail($"Subscription '{name}' configuration not valid: {result}");
        }

        return ValidateOptionsResult.Success;
    }

    private static string? ValidateReplication(ReplicationOptions options)
    {
        string? result = null;

        // replication enabled
        if (options.ConvertHandlerType != null)
        {
            result = PublicationOptionsValidate.ValidateInternal(options);
        }

        return result;
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
            if (options.IsTopicNameWithPartition())
            {
                options.GetTopicPartitions();
            }
            else
            {
                options.GetTopicNames();
            }
        }
        catch (ArgumentException e)
        {
            return e.Message;
        }

        return null;
    }
}