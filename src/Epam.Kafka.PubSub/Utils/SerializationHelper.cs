// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

namespace Epam.Kafka.PubSub.Utils;

/// <summary>
///     Helper to work with default serializers and deserializers for kafka messages.
/// </summary>
internal static class SerializationHelper
{
    /// <summary>
    ///     Default serializers for following types:
    ///     <list type="string">Null</list>
    ///     <list type="string">int</list>
    ///     <list type="string">long</list>
    ///     <list type="string">string (UTF8 encoding)</list>
    ///     <list type="string">float</list>
    ///     <list type="string">double</list>
    ///     <list type="string">byte[]</list>
    /// </summary>
    public static IReadOnlyDictionary<Type, object> DefaultSerializers { get; } = new Dictionary<Type, object>
    {
        { typeof(Null), Serializers.Null },
        { typeof(int), Serializers.Int32 },
        { typeof(long), Serializers.Int64 },
        { typeof(string), Serializers.Utf8 },
        { typeof(float), Serializers.Single },
        { typeof(double), Serializers.Double },
        { typeof(byte[]), Serializers.ByteArray }
    };

    /// <summary>
    ///     Default deserializers for following types:
    ///     <list type="string">Null</list>
    ///     <list type="string">int</list>
    ///     <list type="string">long</list>
    ///     <list type="string">string (UTF8 encoding)</list>
    ///     <list type="string">float</list>
    ///     <list type="string">double</list>
    ///     <list type="string">byte[]</list>
    /// </summary>
    public static IReadOnlyDictionary<Type, object> DefaultDeserializers { get; } = new Dictionary<Type, object>
    {
        { typeof(Null), Deserializers.Null },
        { typeof(Ignore), Deserializers.Ignore },
        { typeof(int), Deserializers.Int32 },
        { typeof(long), Deserializers.Int64 },
        { typeof(string), Deserializers.Utf8 },
        { typeof(float), Deserializers.Single },
        { typeof(double), Deserializers.Double },
        { typeof(byte[]), Deserializers.ByteArray }
    };

    /// <summary>
    ///     Try to get default serializer for <typeparamref name="TType" />.
    /// </summary>
    /// <typeparam name="TType">The type for serialization.</typeparam>
    /// <param name="serializer">
    ///     The serializer for type or
    ///     <value>null</value>
    ///     if it is not available in <see cref="DefaultSerializers" />
    /// </param>
    /// <returns>Whether default serializer available.</returns>
    public static bool TryGetDefaultSerializer<TType>(out ISerializer<TType>? serializer)
    {
        if (DefaultSerializers.TryGetValue(typeof(TType), out object? value))
        {
            serializer = (ISerializer<TType>)value;
            return true;
        }

        serializer = null;
        return false;
    }

    /// <summary>
    ///     Try to get default deserializer for <typeparamref name="TType" />.
    /// </summary>
    /// <typeparam name="TType">The type for deserialization.</typeparam>
    /// <param name="deserializer">
    ///     The deserializer for type or
    ///     <value>null</value>
    ///     if it is not available in <see cref="DefaultDeserializers" />
    /// </param>
    /// <returns>Whether default deserializer available.</returns>
    public static bool TryGetDefaultDeserializer<TType>(out IDeserializer<TType>? deserializer)
    {
        if (DefaultSerializers.TryGetValue(typeof(TType), out object? value))
        {
            deserializer = (IDeserializer<TType>)value;
            return true;
        }

        deserializer = null;
        return false;
    }
}