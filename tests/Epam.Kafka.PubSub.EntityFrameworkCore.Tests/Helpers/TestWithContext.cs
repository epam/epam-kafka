// Copyright © 2024 EPAM Systems

using Epam.Kafka.Tests.Common;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Xunit.Abstractions;

namespace Epam.Kafka.PubSub.EntityFrameworkCore.Tests.Helpers;

public abstract class TestWithContext : TestWithServices
{
    protected TestWithContext(ITestOutputHelper output) : base(output)
    {
#if NET462
        this.Services.AddDbContext<TestContext>(builder =>
            builder.UseInMemoryDatabase(Guid.NewGuid().ToString("N")), ServiceLifetime.Scoped, ServiceLifetime.Singleton);
#else
        this.Services.AddDbContextFactory<TestContext>(builder =>
            builder.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));
#endif

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