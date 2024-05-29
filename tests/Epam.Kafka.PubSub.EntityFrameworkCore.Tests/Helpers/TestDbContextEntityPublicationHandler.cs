// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Publication;
using Epam.Kafka.Tests.Common;

using Microsoft.Extensions.Logging;

#if EF6
using Epam.Kafka.PubSub.EntityFramework6.Publication.Contracts;

namespace Epam.Kafka.PubSub.EntityFramework6.Tests.Helpers;
#else
using Epam.Kafka.PubSub.EntityFrameworkCore.Publication.Contracts;

namespace Epam.Kafka.PubSub.EntityFrameworkCore.Tests.Helpers;
#endif

public class
    TestDbContextEntityPublicationHandler : DbContextEntityPublicationHandler<string, TestEntityKafka, TestEntityDb,
        TestContext>
{
    public TestDbContextEntityPublicationHandler(TestContext context,
        ILogger<TestDbContextEntityPublicationHandler> logger) : base(context,
        logger)
    {
    }

    public TestContext ContextInternal => this.Context;

    protected override IOrderedQueryable<TestEntityDb> OrderBy(IQueryable<TestEntityDb> query)
    {
        return query.OrderBy(x => x.Id);
    }

    protected override IEnumerable<TopicMessage<string, TestEntityKafka>> Convert(TestEntityDb entity)
    {
        return this.ConvertMany(entity);
    }

    public virtual IEnumerable<TopicMessage<string, TestEntityKafka>> ConvertMany(TestEntityDb entity)
    {
        yield return this.ConvertSingle(entity);
    }

    public virtual TopicMessage<string, TestEntityKafka> ConvertSingle(TestEntityDb entity)
    {
        return new TopicMessage<string, TestEntityKafka>
        { Key = entity.ExternalId, Value = new TestEntityKafka { Id = entity.ExternalId } };
    }

    protected override void ErrorCallback(TestEntityDb entity, DeliveryReport report)
    {
        base.ErrorCallback(entity, report);

        entity.KafkaPubErrorCode = (int)report.Error.Code;
        entity.KafkaPubErrorReason = report.Error.Reason;
    }

    protected override void SuccessCallback(TestEntityDb entity, IReadOnlyCollection<DeliveryReport> reports,
        DateTimeOffset? transactionEnd)
    {
        base.SuccessCallback(entity, reports, transactionEnd);

        DeliveryReport report = reports.First();

        entity.KafkaPubOffset = report.Offset;
        entity.KafkaPubPartition = report.Partition;
        entity.KafkaPubTopic = report.Topic;
    }
}