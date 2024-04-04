// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Common;
using Epam.Kafka.PubSub.Subscription;
using Epam.Kafka.PubSub.Subscription.Options;

using Moq;

using Polly;

using Xunit;

namespace Epam.Kafka.PubSub.Tests;

public class PubSubContextTests
{
    [Fact]
    public void Bulkhead()
    {
        const int delay = 4000;

        var options = new SubscriptionOptions
        {
            HandlerConcurrencyGroup = 1,
            HandlerTimeout = TimeSpan.FromMilliseconds(5 * delay)
        };

        PubSubContext context = new ();
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