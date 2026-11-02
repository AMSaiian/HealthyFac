namespace HealthyFac.Identity.IntegrationTests.TokenExchange.Data;

/// <summary>
/// Shares one <see cref="TokenExchangeFixture"/>, and with it one Keycloak and one Redis container, across the token
/// exchange test classes. The classes run one after another, so a test that disables the supervisor for a moment
/// never overlaps another exchange.
/// </summary>
[CollectionDefinition(Name)]
public sealed class TokenExchangeCollection : ICollectionFixture<TokenExchangeFixture>
{
    public const string Name = "TokenExchange";
}
