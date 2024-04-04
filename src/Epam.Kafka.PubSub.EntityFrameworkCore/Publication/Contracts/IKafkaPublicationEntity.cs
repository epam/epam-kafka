﻿// Copyright © 2024 EPAM Systems

namespace Epam.Kafka.PubSub.EntityFrameworkCore.Publication.Contracts;

/// <summary>
///     Interface to define entity that use <see cref="KafkaPublicationState" /> for publication state management.
/// </summary>
public interface IKafkaPublicationEntity
{
    /// <summary>
    ///     The <see cref="KafkaPublicationState" />.
    /// </summary>
    KafkaPublicationState KafkaPubState { get; set; }

    /// <summary>
    ///     UTC DateTime before which entity should not be picked up by publisher.
    /// </summary>
    DateTime KafkaPubNbf { get; set; }
}