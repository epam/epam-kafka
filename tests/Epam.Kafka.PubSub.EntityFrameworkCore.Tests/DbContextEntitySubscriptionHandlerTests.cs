// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.PubSub.EntityFrameworkCore.Tests.Helpers;
using Epam.Kafka.Tests.Common;

using Microsoft.Extensions.DependencyInjection;

using Xunit;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.EntityFrameworkCore.Tests;

public class DbContextEntitySubscriptionHandlerTests : TestWithContext
{
    public DbContextEntitySubscriptionHandlerTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public void CreateSingle()
    {
        this.Services.AddScoped<TestDbContextEntitySubscriptionHandler>();

        using IServiceScope scope = this.ServiceProvider.CreateScope();

        TestDbContextEntitySubscriptionHandler handler =
            scope.ServiceProvider.GetRequiredService<TestDbContextEntitySubscriptionHandler>();

        handler.Execute(new[]
        {
            new ConsumeResult<string, TestEntityKafka>
            {
                Offset = 1,
                Partition = 0,
                Topic = "qwe",
                Message = new Message<string, TestEntityKafka> { Key = "k1", Value = new TestEntityKafka { Id = "k1" } }
            }
        }, CancellationToken.None);

        TestEntityDb entity = scope.ServiceProvider.GetRequiredService<TestContext>().Set<TestEntityDb>().Single();

        Assert.Equal("k1", entity.ExternalId);
        Assert.Equal("Name for k1", entity.Name);
    }
}