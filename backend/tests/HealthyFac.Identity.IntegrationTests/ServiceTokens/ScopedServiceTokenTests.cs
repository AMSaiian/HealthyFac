using HealthyFac.Identity.IntegrationTests.Realm.Data;
using HealthyFac.Identity.IntegrationTests.ServiceTokens.Data;

namespace HealthyFac.Identity.IntegrationTests.ServiceTokens;

public sealed class ScopedServiceTokenTests(ScopedServiceTokenFixture fixture)
    : IClassFixture<ScopedServiceTokenFixture>
{
    private readonly ScopedServiceTokenFixture _fixture = fixture;

    [Fact]
    public async Task ServiceToken_WithOneDownstreamScope_CarriesOnlyItsPermission()
    {
        var claims = await _fixture.RequestServiceTokenClaimsAsync(ScopedServiceTokenTestData.FirstScope);

        claims.Permissions().Should().Equal(ScopedServiceTokenTestData.FirstPermission);
        claims.Audiences().Should().Contain(RealmTestData.CoreClientId);
    }

    [Fact]
    public async Task ServiceToken_WithTwoDownstreamScopes_CarriesBothPermissions()
    {
        var claims = await _fixture.RequestServiceTokenClaimsAsync(ScopedServiceTokenTestData.BothScopes);

        claims.Permissions()
            .Should()
            .BeEquivalentTo(ScopedServiceTokenTestData.FirstPermission, ScopedServiceTokenTestData.SecondPermission);
    }

    [Fact]
    public async Task ServiceToken_WithoutDownstreamScope_CarriesNoPermission()
    {
        var claims = await _fixture.RequestServiceTokenClaimsAsync(null);

        claims.Select(claim => claim.Key).Should().NotContain(RealmTestData.PermissionsClaim);
    }
}
