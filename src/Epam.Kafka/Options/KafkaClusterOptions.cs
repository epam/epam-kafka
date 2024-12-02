// Copyright © 2024 EPAM Systems

using Confluent.Kafka;
using Confluent.SchemaRegistry;

using Microsoft.Extensions.Options;

namespace Epam.Kafka.Options;

/// <summary>
///     Options for KAFKA cluster.
/// </summary>
public sealed class KafkaClusterOptions : IOptions<KafkaClusterOptions>
{
    /// <summary>
    ///     The <see cref="Confluent.Kafka.ClientConfig" />.
    /// </summary>
    public ClientConfig ClientConfig { get; set; } = new();

    /// <summary>
    ///     The <see cref="Confluent.SchemaRegistry.SchemaRegistryConfig" />
    /// </summary>
    public SchemaRegistryConfig SchemaRegistryConfig { get; set; } = new();

    internal bool UsedByFactory { get; set; }

    internal Action<IClient, string?>? OauthHandler { get; private set; }
    internal bool OauthHandlerThrow { get; private set; }
    internal IAuthenticationHeaderValueProvider? AuthenticationHeaderValueProvider { get; set; }

    KafkaClusterOptions IOptions<KafkaClusterOptions>.Value => this;

    /// <summary>
    ///     Set SASL/OAUTHBEARER token refresh callback.
    ///     It is triggered whenever OAUTHBEARER is the SASL
    ///     mechanism and a token needs to be retrieved.
    /// </summary>
    /// <remarks>Warning: https://github.com/confluentinc/confluent-kafka-dotnet/issues/2329</remarks>
    /// <param name="createToken">
    ///     Function with following parameters:
    ///     <list type="string">Input: value of configuration property 'sasl.oauthbearer.config'.</list>
    ///     <list type="string">Output: Token refresh result represented by <see cref="OAuthRefreshResult" />.</list>
    /// </param>
    /// <param name="throwIfAlreadySet">Whether to throw exception if OAuth handler already set.</param>
    /// <returns>The <see cref="KafkaClusterOptions" />.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public KafkaClusterOptions WithOAuthHandler(Func<string?, OAuthRefreshResult> createToken, bool throwIfAlreadySet = false)
    {
        if (createToken == null)
        {
            throw new ArgumentNullException(nameof(createToken));
        }

        this.OauthHandlerThrow = throwIfAlreadySet;
        this.OauthHandler = (client, s) =>
        {
#pragma warning disable CA1031 // catch all exceptions and invoke error handler according to kafka client requirements
            try
            {
                OAuthRefreshResult result = createToken(s);

                client.OAuthBearerSetToken(result.TokenValue,
                    Timestamp.DateTimeToUnixTimestampMs(result.ExpiresAt.UtcDateTime), result.PrincipalName,
                    result.Extensions);
            }
            catch (Exception exception)
            {
                client.OAuthBearerSetTokenFailure(exception.Message);
            }
#pragma warning restore CA1031
        };

        return this;
    }

    /// <summary>
    ///     Set authentication header value provider for schema registry associated with cluster.
    /// </summary>
    /// <param name="provider">The <see cref="IAuthenticationHeaderValueProvider" /> value.</param>
    /// <returns>The <see cref="KafkaClusterOptions" />.</returns>
    /// <exception cref="ArgumentNullException"></exception>
    public KafkaClusterOptions WithSchemaRegistryAuthenticationHeader(IAuthenticationHeaderValueProvider provider)
    {
        this.AuthenticationHeaderValueProvider = provider ?? throw new ArgumentNullException(nameof(provider));

        return this;
    }
}