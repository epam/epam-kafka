// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka.PubSub.Subscription.Pipeline;
using Epam.Kafka.PubSub.Tests.Helpers;
using Epam.Kafka.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.IntegrationTests.Subscription.InternalState;

[Collection(SubscribeTests.Name)]
public class HandlerErrorTests : TestWithServices
{
    private readonly MockCluster _mockCluster;

    public HandlerErrorTests(ITestOutputHelper output, MockCluster mockCluster) : base(output)
    {
        this._mockCluster = mockCluster ?? throw new ArgumentNullException(nameof(mockCluster));
    }
    
    [Fact]
    public async Task TransientErrorWithAdaptiveBatchSize()
    {
        Exception exception = new TestException();

        TopicPartition tp3 = new(this.AnyTopicName, 3);

        using TestObserver observer = new(this, 8);

        var handler = new TestSubscriptionHandler(observer);
        var deserializer = new TestDeserializer(observer);

        this.Services.AddScoped(_ => handler);

        observer.CreateDefaultSubscription(this._mockCluster).WithValueDeserializer(_ => deserializer).WithOptions(x =>
            {
                x.BatchSize = 6;
            });

        Dictionary<TestEntityKafka, TopicPartitionOffset> m = await MockCluster.SeedKafka(this, 10, tp3);

        deserializer.WithSuccess(2, m.Keys.Take(6).ToArray());
        handler.WithError(2, exception, m.Take(6));

        deserializer.WithSuccess(3, m.Keys.Skip(5).Take(1).ToArray());
        handler.WithError(3, exception, m.Take(6));

        deserializer.WithSuccess(4, m.Keys.Skip(5).Take(1).ToArray());
        handler.WithError(4, exception, m.Take(3));

        deserializer.WithSuccess(5, m.Keys.Skip(5).Take(1).ToArray());
        handler.WithError(5, exception, m.Take(1));

        deserializer.WithSuccess(6, m.Keys.Skip(5).Take(1).ToArray());
        handler.WithSuccess(6, m.Take(1));

        deserializer.WithSuccess(7, m.Keys.Skip(6).Take(1).ToArray());
        handler.WithSuccess(7, m.Skip(1).Take(6));

        deserializer.WithSuccess(8, m.Keys.Skip(7).Take(3).ToArray());
        handler.WithSuccess(8, m.Skip(7).Take(3));

        await this.RunBackgroundServices();

        deserializer.Verify();
        handler.Verify();

        // iteration 1
        observer.AssertSubNotAssigned();

        // iteration 2
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(6);
        observer.AssertProcess();
        observer.AssertStop(exception);

        // iteration 3
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(1);
        observer.AssertProcess();
        observer.AssertStop(exception);

        // iteration 4
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(1);
        observer.AssertProcess();
        observer.AssertStop(exception);

        // iteration 5
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(1);
        observer.AssertProcess();
        observer.AssertStop(exception);

        // iteration 6
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(1);
        observer.AssertProcess();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 7
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(1);
        observer.AssertProcess();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);

        // iteration 8
        observer.AssertStart();
        observer.AssertAssign();
        observer.AssertRead(3);
        observer.AssertProcess();
        observer.AssertCommitKafka();
        observer.AssertStop(SubscriptionBatchResult.Processed);
    }
}