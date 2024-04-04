// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Publication;
using Epam.Kafka.Tests.Common;

using Moq;
using Moq.Language.Flow;

namespace Epam.Kafka.PubSub.Tests.Helpers;

public class TestPublicationHandler : IterationMock<TestPublicationHandler.ITestPublicationHandler>,
    IPublicationHandler<string, TestEntityKafka>
{
    private readonly bool _transaction;

    public TestPublicationHandler(bool transaction, TestObserver observer) : base(observer)
    {
        this._transaction = transaction;
    }

    public void TransactionCommitted(CancellationToken cancellationToken)
    {
        this.Mock.Object.TransactionCommitted(cancellationToken);
    }

    public IReadOnlyCollection<TopicMessage<string, TestEntityKafka>> GetBatch(int count, bool transaction,
        CancellationToken cancellationToken)
    {
        return this.Mock.Object.GetBatch(count, transaction, cancellationToken);
    }

    public void ReportResults(IDictionary<TopicMessage<string, TestEntityKafka>, DeliveryReport> reports,
        DateTimeOffset? transactionEnd, CancellationToken cancellationToken)
    {
        foreach (KeyValuePair<TopicMessage<string, TestEntityKafka>, DeliveryReport> report in reports)
        {
            this.Mock.Object.ReportItem(report.Key.Value.Id, report.Value);
        }
    }

    public TestPublicationHandler WithBatch(int iteration, int count,
        params TopicMessage<string, TestEntityKafka>[] messages)
    {
        Mock<ITestPublicationHandler> mock = this.SetupForIteration(iteration);

        mock.Setup(x => x.GetBatch(count, this._transaction, It.IsAny<CancellationToken>())).Returns(messages)
            .Verifiable(Times.Once);

        if (!this._transaction)
        {
            mock.Setup(x => x.TransactionCommitted(It.IsAny<CancellationToken>())).Verifiable(Times.Never);

            mock.Setup(x =>
                x.ReportResults(It.IsAny<IDictionary<TopicMessage<string, TestEntityKafka>, DeliveryReport>>(),
                    It.Is<DateTimeOffset?>(v => v != null), It.IsAny<CancellationToken>())).Verifiable(Times.Never);
        }

        return this;
    }

    public TestPublicationHandler WithReport(int iteration, params KeyValuePair<string, DeliveryReport>[] reports)
    {
        Mock<ITestPublicationHandler> mock = this.SetupForIteration(iteration);

        foreach (KeyValuePair<string, DeliveryReport> p in reports)
        {
            mock.Setup(x => x.ReportItem(p.Key, It.Is<DeliveryReport>(r => p.Value.Status == r.Status
                                                                           && p.Value.Error.Code == r.Error.Code
                                                                           && p.Value.TopicPartitionOffset ==
                                                                           r.TopicPartitionOffset)))
                .Verifiable(Times.Once);
        }

        return this;
    }

    public TestPublicationHandler WithBatch(int iteration, int count, Exception exception)
    {
        Mock<ITestPublicationHandler> mock = this.SetupForIteration(iteration);

        mock.Setup(x => x.GetBatch(count, this._transaction, It.IsAny<CancellationToken>())).Throws(exception)
            .Verifiable(Times.Once);

        return this;
    }

    public TestPublicationHandler WithReport(int iteration, Exception exception)
    {
        Mock<ITestPublicationHandler> mock = this.SetupForIteration(iteration);

        mock.Setup(x => x.ReportResults(It.IsAny<IDictionary<TopicMessage<string, TestEntityKafka>, DeliveryReport>>(),
            It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>())).Throws(exception).Verifiable(Times.Once);

        return this;
    }

    public TestPublicationHandler WithTransaction(int iteration, Exception? exception = null)
    {
        Mock<ITestPublicationHandler> mock = this.SetupForIteration(iteration);

        ISetup<ITestPublicationHandler> setup = mock.Setup(x =>
            x.TransactionCommitted(It.IsAny<CancellationToken>()));

        if (exception != null)
        {
            setup.Throws(exception).Verifiable(Times.Once);
        }
        else
        {
            setup.Verifiable(this._transaction ? Times.Once : Times.Never);
        }

        return this;
    }

    public interface ITestPublicationHandler : IPublicationHandler<string, TestEntityKafka>
    {
        void ReportItem(string? key, DeliveryReport report);
    }
}