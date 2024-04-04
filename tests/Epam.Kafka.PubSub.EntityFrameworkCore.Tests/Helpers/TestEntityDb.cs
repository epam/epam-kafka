// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.EntityFrameworkCore.Publication.Contracts;

using System.ComponentModel.DataAnnotations;

namespace Epam.Kafka.PubSub.EntityFrameworkCore.Tests.Helpers;

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