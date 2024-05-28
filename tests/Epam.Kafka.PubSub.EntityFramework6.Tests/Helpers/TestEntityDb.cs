// Copyright © 2024 EPAM Systems


namespace Epam.Kafka.PubSub.EntityFramework6.Tests.Helpers;

public class TestEntityDb
{
    public long Id { get; set; }
    public string ExternalId { get; set; } = Guid.NewGuid().ToString("N");
    public string? Name { get; set; }
}