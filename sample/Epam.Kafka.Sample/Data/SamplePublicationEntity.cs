// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.EntityFrameworkCore.Publication.Contracts;

using System.ComponentModel.DataAnnotations;

namespace Epam.Kafka.Sample.Data;

public class SamplePublicationEntity : IKafkaPublicationEntity
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [ConcurrencyCheck] public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public KafkaPublicationState KafkaPubState { get; set; }

    public DateTime KafkaPubNbf { get; set; }
}