// Copyright © 2024 EPAM Systems

using System.Data.Entity;

#if EF6

namespace Epam.Kafka.PubSub.EntityFramework6;

internal static class CompatibilityExtensions
{
    public static IQueryable<T> AsTracking<T>(this IQueryable<T> queryable) => queryable;

    public static int SaveChanges(this DbContext context, bool acceptAllChangesOnSuccess) => context.SaveChanges();
}

#endif
