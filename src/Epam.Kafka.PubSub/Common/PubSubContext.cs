// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Common.Options;
using Epam.Kafka.PubSub.Publication.Pipeline;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.PubSub.Utils;

using Microsoft.Extensions.DependencyInjection;

using Polly;

using System.Collections.Concurrent;

namespace Epam.Kafka.PubSub.Common;

/// <summary>
///     Provides information about added subscription and publication background services and objects to monitor their
///     status.
/// </summary>
public sealed class PubSubContext
{
    /// <summary>
    ///     Max number of subscription background services that can be added to <see cref="IServiceCollection" />.
    /// </summary>
    public const int MaxSubscriptionsCount = 100;

    /// <summary>
    ///     Max number of publication background services that can be added to <see cref="IServiceCollection" />.
    /// </summary>
    public const int MaxPublicationsCount = 100;

    private readonly ConcurrentDictionary<int, ISyncPolicy> _bulkheads = new();

    private readonly Dictionary<string, PublicationMonitor> _publications = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SubscriptionMonitor> _subscriptions = new(StringComparer.OrdinalIgnoreCase);

    internal ConcurrentDictionary<string, PublicationMonitor> TransactionIds { get; } = new();

    internal PubSubContext()
    {
    }

    /// <summary>
    ///     Added subscriptions and <see cref="SubscriptionMonitor" />.
    /// </summary>
    public IReadOnlyDictionary<string, SubscriptionMonitor> Subscriptions => this._subscriptions;

    /// <summary>
    ///     Added publications and <see cref="PublicationMonitor" />.
    /// </summary>
    public IReadOnlyDictionary<string, PublicationMonitor> Publications => this._publications;

    internal SubscriptionMonitor AddSubscription(string name)
    {
        if (this._subscriptions.ContainsKey(name))
        {
            throw new InvalidOperationException($"Subscription with name '{name}' already added.");
        }

        if (!RegexHelper.PunSubNameRegex.IsMatch(name))
        {
            throw new InvalidOperationException(
                $"Subscription name '{name}' not match '{RegexHelper.PunSubNameRegex}'.");
        }

        if (this._subscriptions.Count < MaxSubscriptionsCount)
        {
            this._subscriptions.Add(name, new SubscriptionMonitor(this, name));
        }
        else
        {
            throw new InvalidOperationException($"Max subscriptions count of {MaxSubscriptionsCount} exceeded.");
        }

        if (!this._bulkheads.IsEmpty)
        {
            throw new InvalidOperationException();
        }

        return this._subscriptions[name];
    }

    internal PublicationMonitor AddPublication(string name)
    {
        if (this._publications.ContainsKey(name))
        {
            throw new InvalidOperationException($"Publication with name '{name}' already added.");
        }

        if (!RegexHelper.PunSubNameRegex.IsMatch(name))
        {
            throw new InvalidOperationException(
                $"Publication name '{name}' not match '{RegexHelper.PunSubNameRegex}'.");
        }

        if (this._publications.Count < MaxPublicationsCount)
        {
            this._publications.Add(name, new PublicationMonitor(this, name));
        }
        else
        {
            throw new InvalidOperationException($"Max publications count of {MaxPublicationsCount} exceeded.");
        }

        if (!this._bulkheads.IsEmpty)
        {
            throw new InvalidOperationException();
        }

        return this._publications[name];
    }

    internal ISyncPolicy GetHandlerPolicy(PubSubOptions options)
    {
        ISyncPolicy result = Policy.Timeout(_ => options.HandlerTimeout, options.HandlerTimeoutStrategy);

        if (options.HandlerConcurrencyGroup.HasValue)
        {
            ISyncPolicy bulkhead =
                this._bulkheads.GetOrAdd(options.HandlerConcurrencyGroup.Value,
                    _ => Policy.Bulkhead(1, this._subscriptions.Count + this._publications.Count - 1));

            result = bulkhead.Wrap(result);
        }

        return result;
    }
}