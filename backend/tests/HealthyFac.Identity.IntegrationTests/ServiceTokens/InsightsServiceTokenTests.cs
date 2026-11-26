using System.Net;
using HealthyFac.Identity.IntegrationTests.Realm.Data;
using HealthyFac.Identity.IntegrationTests.ServiceTokens.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Identity.IntegrationTests.ServiceTokens;

[Collection(KeycloakRealmCollection.Name)]
[Feature(TestFeatures.Identity)]
public sealed class InsightsServiceTokenTests(KeycloakRealmCollectionFixture fixture)
{
    private readonly KeycloakRealmFixture _realm = fixture.Realm;

    [Theory]
    [MemberData(nameof(InsightsServiceTokenTestData.Scopes), MemberType = typeof(InsightsServiceTokenTestData))]
    public async Task ServiceToken_WithScope_CarriesOnlyThePermissionsOfThatScope(
        string caseName,
        string scope,
        string[] expectedPermissions,
        string expectedAudience
    )
    {
        var claims = await RequestClaimsAsync(scope);

        claims.Permissions().Should().BeEquivalentTo(expectedPermissions, caseName);
        claims.Audiences().Should().Contain(expectedAudience);
    }

    [Fact]
    public async Task ServiceToken_WithoutScope_CarriesNoPermission()
    {
        var claims = await RequestClaimsAsync(scope: null);

        claims.Select(claim => claim.Key).Should().NotContain(RealmTestData.PermissionsClaim);
    }

    private async Task<System.Text.Json.Nodes.JsonObject> RequestClaimsAsync(string? scope)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = RealmTestData.InsightsClientId,
            ["client_secret"] = RealmTestData.InsightsClientSecret,
        };

        if (scope is not null)
        {
            form["scope"] = scope;
        }

        var response = await _realm.RequestTokenAsync(form);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return response.AccessTokenClaims();
    }
}
