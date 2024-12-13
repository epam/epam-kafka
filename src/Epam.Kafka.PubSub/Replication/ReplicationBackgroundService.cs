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
    private readonly ReplicationOptions _options;
    private IPublicationTopicWrapper<TPubKey, TPubValue>? _pubTopic;

    public ReplicationBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        IKafkaFactory kafkaFactory,
        ReplicationOptions options,
        SubscriptionOptions subOptions,
        SubscriptionMonitor monitor,
        ILoggerFactory? loggerFactory) : base(
        serviceScopeFactory, kafkaFactory, subOptions, monitor,
        typeof(ReplicationHandler<TSubKey, TSubValue, TPubKey, TPubValue>), loggerFactory)
    {
        this._options = options ?? throw new ArgumentNullException(nameof(options));
    }

    protected override ISubscriptionHandler<TSubKey, TSubValue> CreateHandler(
        IServiceProvider sp,
        ActivityWrapper activitySpan,
        SubscriptionTopicWrapper<TSubKey, TSubValue> topic)
    {
        //TODO: handle exceptions for handler creation like in sub service
        IConvertHandler<TPubKey, TPubValue, ConsumeResult<TSubKey, TSubValue>> convertHandler =
            sp.ResolveRequiredService<IConvertHandler<TPubKey, TPubValue, ConsumeResult<TSubKey, TSubValue>>>(this._options.ConvertHandlerType!);

        this._pubTopic ??=
            this.KafkaFactory.CreatePublicationTopicWrapper<TPubKey, TPubValue>(null!, this.Monitor, this.Logger);

        return new ReplicationHandler<TSubKey, TSubValue, TPubKey, TPubValue>(activitySpan, topic, this._pubTopic, convertHandler);
    }

    public override void Dispose()
    {
        base.Dispose();

        this._pubTopic?.Dispose();
    }
}