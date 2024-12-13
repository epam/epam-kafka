// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Subscription;

namespace Epam.Kafka.PubSub.Replication;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TSubKey"></typeparam>
/// <typeparam name="TSubValue"></typeparam>
/// <typeparam name="TPubKey"></typeparam>
/// <typeparam name="TPubValue"></typeparam>
public sealed class ReplicationBuilder<TSubKey, TSubValue, TPubKey, TPubValue>
{
    /// <summary>
    /// 
    /// </summary>
    public SubscriptionBuilder<TSubKey, TSubValue> Subscription { get; }
    internal ReplicationBuilder(SubscriptionBuilder<TSubKey, TSubValue> subscription)
    {
        this.Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
    }
}