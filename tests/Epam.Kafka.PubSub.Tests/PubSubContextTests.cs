// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Common;
using Epam.Kafka.PubSub.Publication.Pipeline;
using Epam.Kafka.PubSub.Subscription;
using Epam.Kafka.PubSub.Subscription.Options;
using Moq;

using Polly;
using Shouldly;
using Xunit;

namespace Epam.Kafka.PubSub.Tests;

public class PubSubContextTests
{
    [Fact]
    public void DuplicateName()
    {
        PubSubContext context = new PubSubContext();

        context.AddPublication("v1");
        Assert.Throws<InvalidOperationException>(() => context.AddPublication("v1")).Message.ShouldContain("already added.");
        Assert.Throws<InvalidOperationException>(() => context.AddReplication("v1")).Message.ShouldContain("already used by Publication.");
        context.AddSubscription("v1");

        context.AddSubscription("v2");
        Assert.Throws<InvalidOperationException>(() => context.AddSubscription("v2")).Message.ShouldContain("already added.");
        Assert.Throws<InvalidOperationException>(() => context.AddReplication("v2")).Message.ShouldContain("already used by Subscription.");
        context.AddPublication("v2");

        context.AddReplication("v3");
        Assert.Throws<InvalidOperationException>(() => context.AddReplication("v3")).Message.ShouldContain("already added.");
        Assert.Throws<InvalidOperationException>(() => context.AddSubscription("v3")).Message.ShouldContain("already used by Replication.");
        Assert.Throws<InvalidOperationException>(() => context.AddPublication("v3")).Message.ShouldContain("already used by Replication.");
    }

    [Fact]
    public void DuplicateTransactionId()
    {
        PubSubContext context = new PubSubContext();

        ProducerConfig pc1 = new ProducerConfig { TransactionalId = "qwe1" };
        ProducerConfig pc2 = new ProducerConfig { TransactionalId = "qwe2" };

        PublicationMonitor p1 = context.AddPublication("p1");
        PublicationMonitor p2 = context.AddPublication("p2");

        p1.TryRegisterTransactionId(pc1, out _).ShouldBe(true);
        p2.TryRegisterTransactionId(pc2, out _).ShouldBe(true);
        p2.TryRegisterTransactionId(pc1, out _).ShouldBe(false);
    }

    [Fact]
    public void Bulkhead()
    {
        const int delay = 4000;

        var options = new SubscriptionOptions
        {
            HandlerConcurrencyGroup = 1,
            HandlerTimeout = TimeSpan.FromMilliseconds(5 * delay)
        };

        PubSubContext context = new();
        context.AddSubscription("any");
        ISyncPolicy policy = context.GetHandlerPolicy(options);

        var mock = new Mock<ISubscriptionHandler<Ignore, Ignore>>();
        mock.Setup(x => x.Execute(It.IsAny<IReadOnlyCollection<ConsumeResult<Ignore, Ignore>>>(),
            It.IsAny<CancellationToken>())).Callback(() => Thread.Sleep(delay));

        ISubscriptionHandler<Ignore, Ignore> handler = mock.Object;

        // start first handler
        Task.Run(() => policy.Execute(ct => handler.Execute(new List<ConsumeResult<Ignore, Ignore>>(0), ct),
            CancellationToken.None));

        // get same policy from cache and queue second handler
        policy = context.GetHandlerPolicy(options);

        Thread.Sleep(delay / 4);

        Task.Run(() => policy.Execute(ct => handler.Execute(new List<ConsumeResult<Ignore, Ignore>>(0), ct),
            CancellationToken.None));

        // get same policy from cache and fail third handler.
        policy = context.GetHandlerPolicy(options);

        Thread.Sleep(delay / 4);

        PolicyResult result = policy.ExecuteAndCapture(
            ct => handler.Execute(new List<ConsumeResult<Ignore, Ignore>>(0), ct), CancellationToken.None);

        Assert.Equal(OutcomeType.Failure, result.Outcome);
    }
}