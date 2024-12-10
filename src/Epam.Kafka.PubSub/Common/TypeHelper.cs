// Copyright © 2024 EPAM Systems

using Epam.Kafka.PubSub.Utils;

using Microsoft.Extensions.DependencyInjection;

namespace Epam.Kafka.PubSub.Common;

internal static class TypeHelper
{
    public static T ResolveRequiredService<T>(this IServiceProvider sp, Type type)
        where T : notnull
    {
        try
        {
            return (T)sp.GetRequiredService(type);
        }
        catch (Exception e)
        {
            if (e.Source == "Microsoft.Extensions.DependencyInjection")
            {
                e.DoNotRetryPipeline();
            }
            else
            {
                e.DoNotRetryBatch();
            }

            throw;
        }
    }
}