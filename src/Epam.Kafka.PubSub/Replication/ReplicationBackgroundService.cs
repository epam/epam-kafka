// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Common;
using Epam.Kafka.PubSub.Publication.Topics;
using Epam.Kafka.PubSub.Subscription;
using Epam.Kafka.PubSub.Subscription.Options;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.PubSub.Subscription.Topics;
using Epam.Kafka.PubSub.Utils;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Epam.Kafka.PubSub.Replication;

internal sealed class ReplicationBackgroundService<TSubKey, TSubValue, TPubKey, TPubValue> : SubscriptionBackgroundService<TSubKey, TSubValue>
{
    private IPublicationTopicWrapper<TPubKey, TPubValue>? _pubTopic;

    public ReplicationBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        IKafkaFactory kafkaFactory,
        SubscriptionOptions options,
        SubscriptionMonitor monitor,
        ILoggerFactory? loggerFactory) : base(
        serviceScopeFactory, kafkaFactory, options, monitor,
        typeof(ReplicationHandler<TSubKey, TSubValue, TPubKey, TPubValue>), loggerFactory)
    {
    }

    protected override ISubscriptionHandler<TSubKey, TSubValue> CreateHandler(
        IServiceProvider sp,
        ActivityWrapper activitySpan,
        SubscriptionTopicWrapper<TSubKey, TSubValue> topic)
    {
        IConvertHandler<TPubKey, TPubValue, ConsumeResult<TSubKey, TSubValue>> convertHandler =
            sp.ResolveRequiredService<IConvertHandler<TPubKey, TPubValue, ConsumeResult<TSubKey, TSubValue>>>(this.Options.Replication.ConvertHandlerType!);

        // recreate publisher if needed
        if (this._pubTopic?.Disposed ?? false)
        {
            this._pubTopic = null;
        }

        this._pubTopic ??=
            this.KafkaFactory.CreatePublicationTopicWrapper<TPubKey, TPubValue>(this.Options, this.Monitor, this.Logger);

        return new ReplicationHandler<TSubKey, TSubValue, TPubKey, TPubValue>(activitySpan, topic, this._pubTopic, convertHandler);
    }

    public override void Dispose()
    {
        base.Dispose();

        if (this._pubTopic is { Disposed: false })
        {
            this._pubTopic.Dispose();
        }
    }
}