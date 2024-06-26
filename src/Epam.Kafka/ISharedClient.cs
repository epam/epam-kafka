// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

namespace Epam.Kafka;

/// <summary>
/// Extend <see cref="IClient"/> by adding <see cref="IObservable{T}"/> for <see cref="Error"/> and <see cref="Statistics"/>.
/// </summary>
public interface ISharedClient : IObservable<Error>, IObservable<Statistics>, IClient
{

}