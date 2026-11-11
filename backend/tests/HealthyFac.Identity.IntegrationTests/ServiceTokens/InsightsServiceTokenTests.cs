using System.Net;
using HealthyFac.Identity.IntegrationTests.Realm.Data;
using HealthyFac.Identity.IntegrationTests.ServiceTokens.Data;

namespace HealthyFac.Identity.IntegrationTests.ServiceTokens;

[Collection(KeycloakRealmCollection.Name)]
public sealed class InsightsServiceTokenTests(KeycloakRealmCollectionFixture fixture)
{
    private readonly KeycloakRealmFixture _realm = fixture.Realm;

    [Fact]
    public async Task ServiceToken_WithFilesScope_CarriesOnlyTheFilesReadPermission()
    {
        var claims = await RequestClaimsAsync(InsightsServiceTokenTestData.FilesScope);

        claims.Permissions().Should().BeEquivalentTo(InsightsServiceTokenTestData.FilesPermissions);
        claims.Audiences().Should().Contain(RealmTestData.DocumentClientId);
    }

    [Fact]
    public async Task ServiceToken_WithTelemetryScope_CarriesOnlyTheTwoTelemetryReads()
    {
        var claims = await RequestClaimsAsync(InsightsServiceTokenTestData.TelemetryScope);

        claims.Permissions().Should().BeEquivalentTo(InsightsServiceTokenTestData.TelemetryPermissions);
        claims.Audiences().Should().Contain(RealmTestData.TelemetryClientId);
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
            [key: "grant_type"] = "client_credentials",
            [key: "client_id"] = RealmTestData.InsightsClientId,
            [key: "client_secret"] = RealmTestData.InsightsClientSecret,
        };

        if (scope is not null)
        {
            form[key: "scope"] = scope;
        }

        var response = await _realm.RequestTokenAsync(form);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return response.AccessTokenClaims();
    }
}
