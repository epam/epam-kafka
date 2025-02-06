// Copyright © 2024 EPAM Systems

namespace Epam.Kafka;

/// <summary>
/// Flags representing debug contexts in librdkafka.
/// </summary>
[Flags]
public enum DebugContext
{
    /// <summary>
    /// No debug context.
    /// </summary>
    None = 0x0,

    /// <summary>
    /// Generic (non-specific) debug context.
    /// </summary>
    Generic = 0x1,

    /// <summary>
    /// Broker-related debugging.
    /// </summary>
    Broker = 0x2,

    /// <summary>
    /// Topic-related debugging.
    /// </summary>
    Topic = 0x4,

    /// <summary>
    /// Metadata-related debugging.
    /// </summary>
    Metadata = 0x8,

    /// <summary>
    /// Feature-related debugging.
    /// </summary>
    Feature = 0x10,

    /// <summary>
    /// Queue-related debugging.
    /// </summary>
    Queue = 0x20,

    /// <summary>
    /// Message-related debugging.
    /// </summary>
    Msg = 0x40,

    /// <summary>
    /// Protocol-related debugging.
    /// </summary>
    Protocol = 0x80,

    /// <summary>
    /// Consumer group-related debugging.
    /// </summary>
    Cgrp = 0x100,

    /// <summary>
    /// Security-related debugging.
    /// </summary>
    Security = 0x200,

    /// <summary>
    /// Fetch operation-related debugging.
    /// </summary>
    Fetch = 0x400,

    /// <summary>
    /// Interceptor-related debugging.
    /// </summary>
    Interceptor = 0x800,

    /// <summary>
    /// Plugin-related debugging.
    /// </summary>
    Plugin = 0x1000,

    /// <summary>
    /// Consumer-related debugging.
    /// </summary>
    Consumer = 0x2000,

    /// <summary>
    /// Admin-related debugging.
    /// </summary>
    Admin = 0x4000,

    /// <summary>
    /// Exactly-once semantics (EOS) debugging.
    /// </summary>
    Eos = 0x8000,

    /// <summary>
    /// Mock-related debugging.
    /// </summary>
    Mock = 0x10000,

    /// <summary>
    /// Assignor-related debugging.
    /// </summary>
    Assignor = 0x20000,

    /// <summary>
    /// Configuration-related debugging.
    /// </summary>
    Conf = 0x40000,

    /// <summary>
    /// Telemetry-related debugging.
    /// </summary>
    Telemetry = 0x80000,

    /// <summary>
    /// All available debug contexts.
    /// </summary>
    All = 0xfffff
}