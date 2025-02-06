// Copyright © 2024 EPAM Systems

using System.Text.Json.Serialization;

namespace Epam.Kafka.Stats.Broker;

/// <summary>
/// Enum representing the state of a Broker in librdkafka.
/// See https://github.com/confluentinc/librdkafka/blob/master/src/rdkafka_broker.h
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<BrokerState>))]
public enum BrokerState
{
    /// <summary>
    /// Not available
    /// </summary>
    None,

    /// <summary>
    /// Initial state before any connection attempts.
    /// </summary>
    [JsonStringEnumMemberName("INIT")]
    Init,

    /// <summary>
    /// Broker is not connected.
    /// </summary>
    [JsonStringEnumMemberName("DOWN")]
    Down,

    /// <summary>
    /// Broker is trying to connect.
    /// </summary>
    [JsonStringEnumMemberName("TRY_CONNECT")]
    TryConnect,

    /// <summary>
    /// Broker is connecting.
    /// </summary>
    [JsonStringEnumMemberName("CONNECT")]
    Connect,

    /// <summary>
    /// SSL handshake is underway.
    /// </summary>
    [JsonStringEnumMemberName("SSL_HANDSHAKE")]
    SslHandshake,

    /// <summary>
    /// Broker is using legacy authentication.
    /// </summary>
    [JsonStringEnumMemberName("AUTH_LEGACY")]
    AuthLegacy,

    /// <summary>
    /// Broker is operational for Kafka protocol operations.
    /// </summary>
    [JsonStringEnumMemberName("UP")]
    Up,

    /// <summary>
    /// Broker state is being updated.
    /// </summary>
    [JsonStringEnumMemberName("UPDATE")]
    Update,

    /// <summary>
    /// Broker is querying for supported API versions.
    /// </summary>
    [JsonStringEnumMemberName("APIVERSION_QUERY")]
    ApiVersionQuery,

    /// <summary>
    /// Authentication handshake is in progress.
    /// </summary>
    [JsonStringEnumMemberName("AUTH_HANDSHAKE")]
    AuthHandshake,

    /// <summary>
    /// Authentication request is being processed.
    /// </summary>
    [JsonStringEnumMemberName("AUTH_REQ")]
    AuthReq,

    /// <summary>
    /// Broker is re-authenticating.
    /// </summary>
    [JsonStringEnumMemberName("REAUTH")]
    Reauth,
}