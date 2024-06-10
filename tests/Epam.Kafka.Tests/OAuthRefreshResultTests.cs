// Copyright © 2024 EPAM Systems

using Xunit;

namespace Epam.Kafka.Tests;

public class OAuthRefreshResultTests
{
    [Fact]
    public void ArgumentExceptions()
    {
        Assert.Throws<ArgumentNullException>(() => new OAuthRefreshResult(null!, DateTimeOffset.Now, "qwe"));
        Assert.Throws<ArgumentNullException>(() => new OAuthRefreshResult("qwe", DateTimeOffset.Now, null!));
    }

    [Fact]
    public void Arguments()
    {
        DateTimeOffset anyDto = DateTimeOffset.Now;
        string anyToken = "t";
        string anyPrincipalName = "pn";

        OAuthRefreshResult r1 = new(anyToken, anyDto, anyPrincipalName);

        Assert.Equal(anyToken, r1.TokenValue);
        Assert.Equal(anyDto, r1.ExpiresAt);
        Assert.Equal(anyPrincipalName, r1.PrincipalName);
    }
}