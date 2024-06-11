// Copyright © 2024 EPAM Systems

using Confluent.Kafka;

using Epam.Kafka.Internals;

using Microsoft.Extensions.Configuration;

using System.Globalization;
using System.Runtime.InteropServices;

namespace Epam.Kafka.Options.Configuration;

internal class ClusterOptionsConfigure : OptionsFromConfiguration<KafkaClusterOptions>
{
    private const string SchemaRegistryPrefix = "schema.registry";
    private const string SaslKerberosPrefix = "sasl.kerberos";

    public ClusterOptionsConfigure(IConfiguration configuration, KafkaBuilder kafkaBuilder) : base(configuration, kafkaBuilder)
    {
    }

    protected override string ParentSectionName => "Kafka:Clusters";

    protected override void ConfigureInternal(KafkaClusterOptions options, Dictionary<string, string> items)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            items = CloneWithFilter(items, SaslKerberosPrefix, false);
        }

        options.ClientConfig = new ClientConfig(CloneWithFilter(items, SchemaRegistryPrefix, false));
        options.SchemaRegistryConfig =
            new SchemaRegistryConfigCustom(CloneWithFilter(items, SchemaRegistryPrefix, true));
    }

    private static Dictionary<string, string> CloneWithFilter(IEnumerable<KeyValuePair<string, string>> values,
        string keyPrefix, bool isMatch)
    {
        return values
            .Where(pair => pair.Key.StartsWith(keyPrefix, true, CultureInfo.InvariantCulture) == isMatch)
            .ToDictionary(x => x.Key, x => x.Value);
    }
}