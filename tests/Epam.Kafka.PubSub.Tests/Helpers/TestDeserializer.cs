// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.Tests.Common;

using Moq;
using Moq.Language.Flow;

using System.Text;

namespace Epam.Kafka.PubSub.Tests.Helpers;

public class TestDeserializer : IterationMock<TestDeserializer.IDeserializerCustom>, IDeserializer<TestEntityKafka>
{
    public TestDeserializer(TestObserver observer) : base(observer)
    {
    }
#nullable disable
    public TestEntityKafka Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return this.Mock.Object.Deserialize(Encoding.UTF8.GetString(data.ToArray()), isNull);
    }
#nullable enable

    private static ISetup<IDeserializerCustom, TestEntityKafka?> SetupDeserializer(
        Mock<IDeserializerCustom> mock, TestEntityKafka entity)
    {
        return mock.Setup(x => x.Deserialize(It.Is<string>(s => s == entity.Id), It.IsAny<bool>()));
    }

    public void WithSuccess(int iteration, params TestEntityKafka[] entities)
    {
        Mock<IDeserializerCustom> mock = this.SetupForIteration(iteration);

        foreach (TestEntityKafka entity in entities)
        {
            SetupDeserializer(mock, entity)
                .Returns<string, bool>((data, isNull) => isNull ? null : new TestEntityKafka { Id = data })
                .Verifiable(Times.Once, $"Deserialize entity with Id '{entity.Id}' at {iteration} iteration");
        }
    }

    public void WithError(int iteration, Exception exception, params TestEntityKafka[] entities)
    {
        Mock<IDeserializerCustom> mock = this.SetupForIteration(iteration);

        foreach (TestEntityKafka entity in entities)
        {
            SetupDeserializer(mock, entity)
                .Throws(exception)
                .Verifiable(Times.Once, $"Deserializer throw exception for entity with Id '{entity.Id}' at {iteration} iteration");
        }
    }

    public interface IDeserializerCustom
    {
        TestEntityKafka? Deserialize(string data, bool isNull);
    }
}