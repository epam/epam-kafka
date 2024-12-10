// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Publication;
using Epam.Kafka.Tests.Common;

using Moq;
using Moq.Language.Flow;

namespace Epam.Kafka.PubSub.Tests.Helpers;

public class TestConversionHandler : IterationMock<IConvertHandler<string, TestEntityKafka, ConsumeResult<string, TestEntityKafka>>>, IConvertHandler<string, TestEntityKafka, ConsumeResult<string, TestEntityKafka>>
{
    public TestConversionHandler(TestObserver observer) : base(observer)
    {
    }

    public IEnumerable<TopicMessage<string, TestEntityKafka>> Convert(ConsumeResult<string, TestEntityKafka> entity)
    {
        return this.Mock.Object.Convert(entity);
    }

    private static
        ISetup<IConvertHandler<string, TestEntityKafka, ConsumeResult<string, TestEntityKafka>>,
            IEnumerable<TopicMessage<string, TestEntityKafka>>> SetupConvert(
            Mock<IConvertHandler<string, TestEntityKafka, ConsumeResult<string, TestEntityKafka>>> mock,
            TestEntityKafka entity)
    {
        return mock.Setup(x =>
            x.Convert(It.Is<ConsumeResult<string, TestEntityKafka>>(v => v.Message.Key == entity.Id)));
    }

    public TestConversionHandler WithSuccess(int iteration, int count = 1, params TestEntityKafka[] entities)
    {
        Mock<IConvertHandler<string, TestEntityKafka, ConsumeResult<string, TestEntityKafka>>> mock = this.SetupForIteration(iteration);

        foreach (TestEntityKafka entity in entities)
        {
            SetupConvert(mock, entity)
                .Returns<ConsumeResult<string, TestEntityKafka>>(x =>
                    Enumerable.Repeat(
                        new TopicMessage<string, TestEntityKafka> { Key = x.Message.Key, Value = x.Message.Value },
                        count))
                .Verifiable(Times.Once, $"Convert entity with Id '{entity.Id}' at {iteration} iteration");
        }

        return this;
    }

    public void WithError(int iteration, Exception exception, params TestEntityKafka[] entities)
    {
        Mock<IConvertHandler<string, TestEntityKafka, ConsumeResult<string, TestEntityKafka>>> mock = this.SetupForIteration(iteration);

        foreach (TestEntityKafka entity in entities)
        {
            SetupConvert(mock, entity)
                .Throws(exception)
                .Verifiable(Times.Once, $"Convert throw exception for entity with Id '{entity.Id}' at {iteration} iteration");
        }
    }
}