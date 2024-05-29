// Copyright © 2024 EPAM Systems

using System.Collections.Generic;
using System.Linq;
using Epam.Kafka.PubSub.EntityFramework6.Publication.Contracts;
using Epam.Kafka.PubSub.Publication;
using Epam.Kafka.Sample.Net462.Data;
using Microsoft.Extensions.Logging;

namespace Epam.Kafka.Sample.Net462.Samples
{
    public class PublicationHandlerSample : DbContextEntityPublicationHandler<string, KafkaEntity, SamplePublicationEntity,
        SampleDbContext>
    {
        public PublicationHandlerSample(SampleDbContext context, ILogger<PublicationHandlerSample> logger) : base(context,
            logger)
        {
        }

        protected override IEnumerable<TopicMessage<string, KafkaEntity>> Convert(SamplePublicationEntity entity)
        {
            yield return new TopicMessage<string, KafkaEntity>
            { Key = entity.Id, Value = new KafkaEntity { Id = entity.Id } };
        }

        protected override IOrderedQueryable<SamplePublicationEntity> OrderBy(IQueryable<SamplePublicationEntity> query)
        {
            return query.OrderBy(x => x.KafkaPubNbf);
        }
    }
}