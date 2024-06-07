// Copyright © 2024 EPAM Systems

using System;
using System.ComponentModel.DataAnnotations;
using Epam.Kafka.PubSub.EntityFramework6.Publication.Contracts;

namespace Epam.Kafka.Sample.Net462.Data
{
    public class SamplePublicationEntity : IKafkaPublicationEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        [ConcurrencyCheck] public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public KafkaPublicationState KafkaPubState { get; set; }

        public DateTime KafkaPubNbf { get; set; }
    }
}