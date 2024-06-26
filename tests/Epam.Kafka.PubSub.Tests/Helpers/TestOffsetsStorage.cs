// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.Subscription;

using Moq;
using Moq.Language.Flow;

namespace Epam.Kafka.PubSub.Tests.Helpers;

public class TestOffsetsStorage : IterationMock<TestOffsetsStorage.ITestExternalState>, IExternalOffsetsStorage
{
    private readonly TopicPartitionOffset[] _ignoredPartitions;

    public TestOffsetsStorage(TestObserver observer, params TopicPartitionOffset[] ignoredPartitions) : base(observer)
    {
        this._ignoredPartitions = ignoredPartitions ?? throw new ArgumentNullException(nameof(ignoredPartitions));
    }

    public TestOffsetsStorage(TestObserver observer, params int[] ignoredPartitions) : base(observer)
    {
        this._ignoredPartitions = ignoredPartitions
            .Select(x => new TopicPartitionOffset(observer.Test.AnyTopicName, x, Offset.End)).ToArray();
    }

    public TestOffsetsStorage(TestObserver observer) : base(observer)
    {
        this._ignoredPartitions = Array.Empty<TopicPartitionOffset>();
    }

    public IReadOnlyCollection<TopicPartitionOffset> CommitOrReset(IReadOnlyCollection<TopicPartitionOffset> offsets,
        string? consumerGroup,
        CancellationToken cancellationToken)
    {
        return offsets.OrderBy(x => x.Topic).ThenBy(x => x.Partition.Value).Select(x => this.Mock.Object.CommitOrReset(x, consumerGroup)).ToList();
    }

    public IReadOnlyCollection<TopicPartitionOffset> GetOrCreate(IReadOnlyCollection<TopicPartition> topics,
        string? consumerGroup, CancellationToken cancellationToken)
    {
        return topics.OrderBy(x => x.Topic).ThenBy(x => x.Partition.Value).Select(x => this.Mock.Object.GetOrCreate(x, consumerGroup)).ToList();
    }

    public void WithGet(int iteration, params TopicPartitionOffset[] offsets)
    {
        Mock<ITestExternalState> mock = this.SetupForIteration(iteration);

        foreach (TopicPartitionOffset? offset in offsets.Concat(this._ignoredPartitions))
        {
            mock.Setup(x =>
                    x.GetOrCreate(
                        It.Is<TopicPartition>(v =>
                            v.Topic == offset.TopicPartition.Topic &&
                            v.Partition.Value == offset.TopicPartition.Partition.Value), It.IsAny<string?>()))
                .Returns(offset)
                .Verifiable(Times.Once, $"GetOrCreate {offset} at {iteration} iteration.");
        }
    }

    public void WithGetError(int iteration, Exception exception)
    {
        Mock<ITestExternalState> mock = this.SetupForIteration(iteration);

        mock.Setup(x => x.GetOrCreate(It.IsAny<TopicPartition>(), It.IsAny<string>())).Throws(exception)
            .Verifiable(Times.Once, $"GetOrCreate error at {iteration} iteration.");
    }

    public void WithSet(int iteration, params TopicPartitionOffset[] offsets)
    {
        Mock<ITestExternalState> mock = this.SetupForIteration(iteration);

        foreach (TopicPartitionOffset? offset in offsets)
        {
            mock.Setup(x =>
                    x.CommitOrReset(
                        It.Is<TopicPartitionOffset>(v =>
                            v.Topic == offset.TopicPartition.Topic &&
                            v.Partition.Value == offset.TopicPartition.Partition.Value &&
                            v.Offset == offset.Offset),
                        It.IsAny<string?>())).Returns(offset)
                .Verifiable(Times.Once, $"CommitOrReset {offset} at {iteration} iteration.");
        }
    }

    public void WithSetError(int iteration, Exception exception, params TopicPartitionOffset[] offsets)
    {
        Mock<ITestExternalState> mock = this.SetupForIteration(iteration);

        int number = 1;

        foreach (TopicPartitionOffset? offset in offsets.OrderBy(x => x.Topic).ThenBy(x => x.Partition.Value))
        {
            ISetup<ITestExternalState, TopicPartitionOffset> setup = mock.Setup(x =>
                x.CommitOrReset(
                    It.Is<TopicPartitionOffset>(v =>
                        v.Topic == offset.TopicPartition.Topic &&
                        v.Partition.Value == offset.TopicPartition.Partition.Value &&
                        v.Offset == offset.Offset),
                    It.IsAny<string?>()));

            if (number == offsets.Length)
            {
                setup
                    .Throws(exception)
                    .Verifiable(Times.Once, $"CommitOrReset error at {iteration} iteration.");
            }
            else
            {
                setup
                    .Returns(offset)
                    .Verifiable(Times.Once, $"CommitOrReset {offset} at {iteration} iteration.");
            }

            number++;
        }
    }

    public void WithSetAndGetForNextIteration(int iteration, params TopicPartitionOffset[] offsets)
    {
        this.WithSet(iteration, offsets);
        this.WithGet(iteration + 1, offsets);
    }

    public interface ITestExternalState : IExternalOffsetsStorage
    {
        TopicPartitionOffset CommitOrReset(TopicPartitionOffset offset, string? consumerGroup);
        TopicPartitionOffset GetOrCreate(TopicPartition tp, string? consumerGroup);
    }

    public void WithReset(int iteration, TopicPartitionOffset from, TopicPartitionOffset to)
    {
        Mock<ITestExternalState> mock = this.SetupForIteration(iteration);

        mock.Setup(x =>
                x.CommitOrReset(
                    It.Is<TopicPartitionOffset>(v =>
                        v.Topic == from.TopicPartition.Topic &&
                        v.Partition.Value == from.TopicPartition.Partition.Value &&
                        v.Offset == from.Offset),
                    It.IsAny<string?>())).Returns(to)
            .Verifiable(Times.Once, $"CommitOrReset from {from} to {to} at {iteration} iteration.");
    }
}