// Copyright © 2024 EPAM Systems

namespace Epam.Kafka;

/// <summary>
///     Represent a result of SASL OAUTHBEARER token refresh
/// </summary>
public class OAuthRefreshResult
{
    /// <summary>
    ///     Initialize the <see cref="OAuthRefreshResult" /> instance.
    /// </summary>
    /// <param name="tokenValue">
    ///     <inheritdoc cref="TokenValue" />
    /// </param>
    /// <param name="principalName">
    ///     <inheritdoc cref="PrincipalName" />
    /// </param>
    /// <param name="expiresAt">
    ///     <inheritdoc cref="ExpiresAt" />
    /// </param>
    /// <param name="extensions">
    ///     <inheritdoc cref="Extensions" />
    /// </param>
    /// <exception cref="ArgumentNullException"></exception>
    public OAuthRefreshResult(string tokenValue, DateTimeOffset expiresAt, string principalName,
        IDictionary<string, string>? extensions = null)
    {
        this.TokenValue = tokenValue ?? throw new ArgumentNullException(nameof(tokenValue));
        this.PrincipalName = principalName ?? throw new ArgumentNullException(nameof(principalName));
        this.ExpiresAt = expiresAt;
        this.Extensions = extensions;
    }

    /// <summary>
    ///     The mandatory token value to set, often (but not necessarily) a JWS compact serialization
    /// </summary>
    public string TokenValue { get; }

    /// <summary>
    ///     The mandatory Kafka principal name associated with the token.
    /// </summary>
    public string PrincipalName { get; }

    /// <summary>
    ///     When the token expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; }

    /// <summary>
    ///     Optional SASL extensions dictionary, to be
    ///     communicated to the broker as additional key-value
    ///     pairs during the initial client response.
    /// </summary>
    public IDictionary<string, string>? Extensions { get; }
}