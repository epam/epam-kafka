﻿// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common;
using Epam.Kafka.PubSub.Subscription.Options;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Epam.Kafka.PubSub.Subscription.Replication;

/// <summary>
///     Fluent API to configure replication background services behaviour.
/// </summary>
/// <typeparam name="TSubKey">The input message key type.</typeparam>
/// <typeparam name="TSubValue">The input message value type.</typeparam>
/// <typeparam name="TPubKey">The output message key type.</typeparam>
/// <typeparam name="TPubValue">The output message value type.</typeparam>
internal sealed class ReplicationBuilder<TSubKey, TSubValue, TPubKey, TPubValue> : SubscriptionBuilder<TSubKey, TSubValue>
{
    internal ReplicationBuilder(KafkaBuilder builder, string name) : base(builder, name, typeof(ReplicationHandler<TSubKey, TSubValue, TPubKey, TPubValue>))
    {
    }

    internal override IHostedService Build(IServiceProvider sp)
    {
        return new ReplicationBackgroundService<TSubKey, TSubValue, TPubKey, TPubValue>(
            sp.GetRequiredService<IServiceScopeFactory>(),
            sp.GetRequiredService<IKafkaFactory>(),
            sp.GetRequiredService<IOptionsMonitor<SubscriptionOptions>>(),
            sp.GetRequiredService<PubSubContext>().Subscriptions[this.Key],
            sp.GetService<ILoggerFactory>());
    }
}