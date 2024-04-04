// Copyright © 2024 EPAM Systems

using PublicApiGenerator;

using Shouldly;

using System.Reflection;

namespace Epam.Kafka.Tests.Common;

public static class PublicApiHelper
{
    public static void ShouldMatchApproved(this Assembly assembly)
    {
        string publicApi = assembly.GeneratePublicApi(new ApiGeneratorOptions
        {
            IncludeAssemblyAttributes = false,
        }) + Environment.NewLine;

        publicApi.ShouldMatchApproved(options =>
            options.UseCallerLocation().WithFilenameGenerator((_, _, fileType, fileExtension) =>
                $"{assembly.GetName().Name}.{fileType}.{fileExtension}"));
    }
}