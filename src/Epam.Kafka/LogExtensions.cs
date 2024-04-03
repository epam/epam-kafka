// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Microsoft.Extensions.Logging;

namespace Epam.Kafka;

/// <summary>
///     Logging extensions.
/// </summary>
public static partial class LogExtensions
{
    /// <summary>
    ///     Convert kafka log level represented by <see cref="SyslogLevel" /> to .net log level represented by
    ///     <see cref="Microsoft.Extensions.Logging.LogLevel" />.
    /// </summary>
    /// <param name="level">The kafka log level to convert.</param>
    /// <returns>The corresponding .net log level.</returns>
    public static LogLevel ToNetLogLevel(this SyslogLevel level)
    {
        LogLevel result = level switch
        {
            SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical => LogLevel.Critical,
            SyslogLevel.Error => LogLevel.Error,
            SyslogLevel.Warning or SyslogLevel.Notice => LogLevel.Warning,
            SyslogLevel.Info => LogLevel.Information,
            SyslogLevel.Debug => LogLevel.Debug,
            _ => LogLevel.None,
        };
        return result;
    }

    /// <summary>
    ///     Write <see cref="LogMessage" /> from kafka log handler to <see cref="ILogger" />.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="msg">The message from kafka log handler.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void KafkaLogHandler(this ILogger logger, LogMessage msg)
    {
        if (msg == null)
        {
            throw new ArgumentNullException(nameof(msg));
        }

        logger.KafkaLogHandler(msg.Level.ToNetLogLevel(), msg.Name, msg.Facility, msg.Message);
    }

    [LoggerMessage(
        EventId = 300,
        EventName = "KafkaLogHandler",
        Message = "KafkaLogHandler {Facility} {ClientName} {Msg}.")]
    static partial void KafkaLogHandler(this ILogger logger, LogLevel level, string clientName, string facility,
        string msg);

    [LoggerMessage(
        EventId = 201,
        EventName = "ConsumerCreated",
        Level = LogLevel.Information,
        Message = "Consumer created. KeyType: {KeyType}, ValueType: {ValueType}) Config: {Config}.")]
    internal static partial void ConsumerCreateOk(this ILogger logger, IEnumerable<KeyValuePair<string, string>> config,
        Type keyType, Type valueType);

    [LoggerMessage(
        EventId = 501,
        EventName = "ConsumerCreateError",
        Level = LogLevel.Error,
        Message = "Consumer create error. KeyType: {KeyType}, ValueType: {ValueType}) Config: {Config}.")]
    internal static partial void ConsumerCreateError(this ILogger logger, Exception exception,
        IEnumerable<KeyValuePair<string, string>> config, Type keyType, Type valueType);

    [LoggerMessage(
        EventId = 202,
        EventName = "ProducerCreated",
        Level = LogLevel.Information,
        Message = "Producer created. KeyType: {KeyType}, ValueType: {ValueType}) Config: {Config}.")]
    internal static partial void ProducerCreateOk(this ILogger logger, IEnumerable<KeyValuePair<string, string>> config,
        Type keyType, Type valueType);

    [LoggerMessage(
        EventId = 502,
        EventName = "ProducerCreateError",
        Level = LogLevel.Error,
        Message = "Producer create error. KeyType: {KeyType}, ValueType: {ValueType}) Config: {Config}.")]
    internal static partial void ProducerCreateError(this ILogger logger, Exception exception,
        IEnumerable<KeyValuePair<string, string>> config, Type keyType, Type valueType);
}