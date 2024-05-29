// Copyright © 2024 EPAM Systems

#if EF6
using Epam.Kafka.PubSub.EntityFramework6.Publication.Contracts;

namespace Epam.Kafka.PubSub.EntityFramework6.Tests.Helpers;
#else
using Epam.Kafka.PubSub.EntityFrameworkCore.Publication.Contracts;

namespace Epam.Kafka.PubSub.EntityFrameworkCore.Tests.Helpers;
#endif

using System.ComponentModel.DataAnnotations;


public class TestEntityDb : IKafkaPublicationEntity
{
    public long Id { get; set; }
    public string ExternalId { get; set; } = Guid.NewGuid().ToString("N");
    public string? Name { get; set; }
    public long KafkaPubOffset { get; set; }
    public int KafkaPubPartition { get; set; }
    public string? KafkaPubErrorReason { get; set; }
    public int KafkaPubErrorCode { get; set; }
    public string? KafkaPubTopic { get; set; }

    [ConcurrencyCheck] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public KafkaPublicationState KafkaPubState { get; set; }
    public DateTime KafkaPubNbf { get; set; }
}