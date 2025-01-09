// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Publication;
using Epam.Kafka.PubSub.Subscription.Replication;
using Epam.Kafka.Tests.Common;

using Moq;
using Moq.Language.Flow;

namespace Epam.Kafka.PubSub.Tests.Helpers;

public class TestConversionHandler : IterationMock<IConvertHandler<string, TestEntityKafka, ConsumeResult<string, TestEntityKafka>>>, IConvertHandler<string, TestEntityKafka, ConsumeResult<string, TestEntityKafka>>
{
    public TestConversionHandler(TestObserver observer) : base(observer)
    {
    }


    public IReadOnlyCollection<TopicMessage<string, TestEntityKafka>> Convert(IReadOnlyCollection<ConsumeResult<string, TestEntityKafka>> entities,
        CancellationToken cancellationToken)
    {
        return this.Mock.Object.Convert(entities, cancellationToken);
    }

    private static
        ISetup<IConvertHandler<string, TestEntityKafka, ConsumeResult<string, TestEntityKafka>>,
            IReadOnlyCollection<TopicMessage<string, TestEntityKafka>>> SetupConvert(
            Mock<IConvertHandler<string, TestEntityKafka, ConsumeResult<string, TestEntityKafka>>> mock,
            ConsumeResult<string, TestEntityKafka>[] entities)
    {
        mock.Setup(x => x.Convert(It.Is<IReadOnlyCollection<ConsumeResult<string, TestEntityKafka>>>(v => v.Count == 0),
            It.IsAny<CancellationToken>())).Verifiable(Times.Never);

        return mock.Setup(x =>
            x.Convert(
                It.Is<IReadOnlyCollection<ConsumeResult<string, TestEntityKafka>>>(results =>
                    results.Count == entities.Length &&
                    results.All(r => entities.Count(e =>
                        r.Message.Key == e.Message.Key
                        && r.Message.Value.Id == e.Message.Value.Id
                       // && r.TopicPartitionOffset == e.TopicPartitionOffset
                    ) == 1)),
                It.IsAny<CancellationToken>()));
    }

    public TestConversionHandler WithSuccess(int iteration, params TestEntityKafka[] entities)
    {
        Mock<IConvertHandler<string, TestEntityKafka, ConsumeResult<string, TestEntityKafka>>> mock = this.SetupForIteration(iteration);

        TopicMessage<string, TestEntityKafka>[] messages = entities.Select(x => new TopicMessage<string, TestEntityKafka> { Key = x.Id, Value = x })
            .ToArray();

        ConsumeResult<string, TestEntityKafka>[] result = messages.Select(x =>
            new ConsumeResult<string, TestEntityKafka> { Message = x }).ToArray();

        SetupConvert(mock, result)
            .Returns(messages)
            .Verifiable(Times.Once, $"Convert entities at {iteration} iteration");

        return this;
    }

    public TestConversionHandler WithEmpty(int iteration, params TestEntityKafka[] entities)
    {
        Mock<IConvertHandler<string, TestEntityKafka, ConsumeResult<string, TestEntityKafka>>> mock = this.SetupForIteration(iteration);

        TopicMessage<string, TestEntityKafka>[] messages = entities.Select(x => new TopicMessage<string, TestEntityKafka> { Key = x.Id, Value = x })
            .ToArray();

        ConsumeResult<string, TestEntityKafka>[] result = messages.Select(x =>
            new ConsumeResult<string, TestEntityKafka> { Message = x }).ToArray();

        SetupConvert(mock, result)
            .Returns(Array.Empty<TopicMessage<string, TestEntityKafka>>())
            .Verifiable(Times.Once, $"Convert entities at {iteration} iteration");

        return this;
    }

    public void WithError(int iteration, Exception exception, params TestEntityKafka[] entities)
    {
        Mock<IConvertHandler<string, TestEntityKafka, ConsumeResult<string, TestEntityKafka>>> mock = this.SetupForIteration(iteration);

        TopicMessage<string, TestEntityKafka>[] messages = entities.Select(x => new TopicMessage<string, TestEntityKafka> { Key = x.Id, Value = x })
            .ToArray();

        ConsumeResult<string, TestEntityKafka>[] result = messages.Select(x =>
            new ConsumeResult<string, TestEntityKafka>
                { Message = x }).ToArray();

        SetupConvert(mock, result)
            .Throws(exception)
            .Verifiable(Times.Once, $"Convert entities throws at {iteration} iteration");
    }
}