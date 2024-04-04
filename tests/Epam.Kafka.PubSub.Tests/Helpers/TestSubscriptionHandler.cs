// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Subscription;
using Epam.Kafka.Tests.Common;

using Moq;
using Moq.Language.Flow;

namespace Epam.Kafka.PubSub.Tests.Helpers;

public class TestSubscriptionHandler : IterationMock<ISubscriptionHandler<string, TestEntityKafka>>,
    ISubscriptionHandler<string, TestEntityKafka>
{
    public TestSubscriptionHandler(TestObserver observer) : base(observer)
    {
    }

    public void Execute(IReadOnlyCollection<ConsumeResult<string, TestEntityKafka>> items,
        CancellationToken cancellationToken)
    {
        this.Mock.Object.Execute(items, cancellationToken);
    }

    private static ISetup<ISubscriptionHandler<string, TestEntityKafka>> SetupHandler(
        Mock<ISubscriptionHandler<string, TestEntityKafka>> mock,
        ConsumeResult<string, TestEntityKafka>[] entities)
    {
        mock.Setup(x => x.Execute(It.Is<IReadOnlyCollection<ConsumeResult<string, TestEntityKafka>>>(v => v.Count == 0),
            It.IsAny<CancellationToken>())).Verifiable(Times.Never);

        return mock.Setup(x =>
            x.Execute(
                It.Is<IReadOnlyCollection<ConsumeResult<string, TestEntityKafka>>>(results =>
                    results.Count == entities.Length &&
                    results.All(r => entities.Count(e =>
                        r.Message.Key == e.Message.Key
                        && r.Message.Value.Id == e.Message.Value.Id
                        && r.TopicPartitionOffset == e.TopicPartitionOffset
                    ) == 1)),
                It.IsAny<CancellationToken>()));
    }

    public void WithSuccess(int iteration, params ConsumeResult<string, TestEntityKafka>[] entities)
    {
        Mock<ISubscriptionHandler<string, TestEntityKafka>> mock = this.SetupForIteration(iteration);

        SetupHandler(mock, entities)
            .Verifiable(Times.Once, $"Handled {entities.Length} entity(s) at {iteration} iteration");
    }

    public void WithSuccess(int iteration,
        IEnumerable<KeyValuePair<TestEntityKafka, TopicPartitionOffset>> entities)
    {
        this.WithSuccess(iteration, entities.Select(x => x.Key.ToConsumeResult(x.Value)).ToArray());
    }

    public void WithError(int iteration, Exception exception,
        IEnumerable<KeyValuePair<TestEntityKafka, TopicPartitionOffset>> entities)
    {
        this.WithError(iteration, exception, entities.Select(x => x.Key.ToConsumeResult(x.Value)).ToArray());
    }

    public void WithError(int iteration, Exception exception,
        params ConsumeResult<string, TestEntityKafka>[] entities)
    {
        Mock<ISubscriptionHandler<string, TestEntityKafka>> mock = this.SetupForIteration(iteration);

        SetupHandler(mock, entities)
            .Throws(exception)
            .Verifiable(Times.Once, $"Throw exception for {entities.Length} entity(s) at {iteration} iteration");
    }
}