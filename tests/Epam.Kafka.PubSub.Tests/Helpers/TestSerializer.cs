// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.Tests.Common;

using Moq;
using Moq.Language.Flow;

namespace Epam.Kafka.PubSub.Tests.Helpers;

public class TestSerializer : IterationMock<ISerializer<TestEntityKafka>>, ISerializer<TestEntityKafka>
{
    public TestSerializer(TestObserver observer) : base(observer)
    {
    }

    public byte[] Serialize(TestEntityKafka data, SerializationContext context)
    {
        return this.Mock.Object.Serialize(data, context);
    }

    private static ISetup<ISerializer<TestEntityKafka>, byte[]> SetupSerializer(
        Mock<ISerializer<TestEntityKafka>> mock, TestEntityKafka entity)
    {
        return mock.Setup(x =>
            x.Serialize(It.Is<TestEntityKafka>(v => v.Id == entity.Id), It.IsAny<SerializationContext>()));
    }

    public TestSerializer WithSuccess(int iteration, params TestEntityKafka[] entities)
    {
        Mock<ISerializer<TestEntityKafka>> mock = this.SetupForIteration(iteration);

        foreach (TestEntityKafka entity in entities)
        {
            SetupSerializer(mock, entity)
                .Returns<TestEntityKafka, SerializationContext>((x, _) => x.GetBytesId())
                .Verifiable(Times.Once, $"Serialize entity with Id '{entity.Id}' at {iteration} iteration");
        }

        return this;
    }

    public void WithError(int iteration, Exception exception, params TestEntityKafka[] entities)
    {
        Mock<ISerializer<TestEntityKafka>> mock = this.SetupForIteration(iteration);

        foreach (TestEntityKafka entity in entities)
        {
            SetupSerializer(mock, entity)
                .Throws(exception)
                .Verifiable(Times.Once, $"Throw exception for entity with Id '{entity.Id}' at {iteration} iteration");
        }
    }
}