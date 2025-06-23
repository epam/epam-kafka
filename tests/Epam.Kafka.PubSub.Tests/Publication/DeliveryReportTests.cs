// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Epam.Kafka.PubSub.Publication;
using Shouldly;
using Xunit;

namespace Epam.Kafka.PubSub.Tests.Publication;
public class DeliveryReportTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        var topic = "test-topic";
        var partition = new Partition(1);
        var offset = new Offset(100);
        var error = new Error(ErrorCode.NoError);
        var status = PersistenceStatus.Persisted;
        var timestamp = new Timestamp(DateTime.UtcNow);

        var report = new DeliveryReport(topic, partition, offset, error, status, timestamp);

        report.Topic.ShouldBe(topic);
        report.Partition.ShouldBe(partition);
        report.Offset.ShouldBe(offset);
        report.Error.ShouldBe(error);
        report.Status.ShouldBe(status);
        report.Timestamp.ShouldBe(timestamp);
    }

    [Fact]
    public void ToString_ShouldReturnExpectedString()
    {
        var topic = "test-topic";
        var partition = new Partition(1);
        var offset = new Offset(100);
        var error = new Error(ErrorCode.NoError);
        var status = PersistenceStatus.Persisted;
        var timestamp = new Timestamp(DateTime.UtcNow);

        var report = new DeliveryReport(topic, partition, offset, error, status, timestamp);

        var expectedString = "test-topic [[1]] @100: Success Persisted";
        report.ToString().ShouldBe(expectedString);
    }

    [Fact]
    public void FromGenericReport_ShouldThrowArgumentNullException_WhenReportIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => DeliveryReport.FromGenericReport<string, string>(null));
    }

    [Fact]
    public void FromGenericReport_ShouldCreateDeliveryReportFromGenericReport()
    {
        var topic = "test-topic";
        var partition = new Partition(1);
        var offset = new Offset(100);
        var error = new Error(ErrorCode.NoError);
        var status = PersistenceStatus.Persisted;
        var timestamp = new Timestamp(DateTime.UtcNow);

        var genericReport = new DeliveryReport<string, string>
        {
            Topic = topic,
            Partition = partition,
            Offset = offset,
            Error = error,
            Status = status,
            Message = new Message<string, string>
            {
                Timestamp = timestamp
            }
        };

        var report = DeliveryReport.FromGenericReport(genericReport);

        report.Topic.ShouldBe(topic);
        report.Partition.ShouldBe(partition);
        report.Offset.ShouldBe(offset);
        report.Error.ShouldBe(error);
        report.Status.ShouldBe(status);
        report.Timestamp.ShouldBe(timestamp);
    }
}