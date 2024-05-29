// Copyright © 2024 EPAM Systems

using Effort.Provider;
using Epam.Kafka.Tests.Common;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.EntityFramework6.Tests.Helpers;

public abstract class TestWithContext : TestWithServices
{
    protected TestWithContext(ITestOutputHelper output) : base(output)
    {
        this.Services.TryAddSingleton(Effort.DbConnectionFactory.CreatePersistent(Guid.NewGuid().ToString("N")));
        this.Services.TryAddScoped(sp => new TestContext(sp.GetRequiredService<EffortConnection>()));
    }

    protected void SeedData(params object[] data)
    {
        using IServiceScope scope = this.ServiceProvider.CreateScope();

        TestContext context = scope.ServiceProvider.GetRequiredService<TestContext>();

        foreach (object entity in data)
        {
            context.Add(entity);
        }

        context.SaveChanges();
    }
}