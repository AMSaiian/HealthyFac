using System.Text.Json.Nodes;
using HealthyFac.Identity.IntegrationTests.Realm.Data;

namespace HealthyFac.Identity.IntegrationTests.ServiceTokens.Data;

/// <summary>
/// Creates the temporary service client of the scoped service token tests in the started realm: two client roles of
/// <c>healthyfac-core</c>, a composite realm role that groups them, and a confidential client whose service account
/// holds that role. The client has <c>fullScopeAllowed: false</c> and one optional client scope for each role, with a
/// role scope mapping.
/// </summary>
public sealed class ScopedServiceTokenFixture
{
    private static readonly string[] ServiceClientDefaultScopes = ["basic", "service_account", "healthyfac-claims"];

    public ScopedServiceTokenFixture(KeycloakRealmFixture realm)
    {
        Realm = realm;
    }

    public KeycloakRealmFixture Realm { get; }

    public async ValueTask InitializeAsync()
    {
        var coreUuid = await Realm.FindClientUuidAsync(RealmTestData.CoreClientId);
        var first = await Realm.CreateClientRoleAsync(coreUuid, ScopedServiceTokenTestData.FirstPermission);
        var second = await Realm.CreateClientRoleAsync(coreUuid, ScopedServiceTokenTestData.SecondPermission);
        var serviceRole = await Realm.CreateRealmRoleAsync(ScopedServiceTokenTestData.ServiceRoleName, first, second);

        var serviceUuid = await Realm.CreateClientAsync(
            new JsonObject
            {
                [propertyName: "clientId"] = ScopedServiceTokenTestData.ServiceClientId,
                [propertyName: "enabled"] = true,
                [propertyName: "publicClient"] = false,
                [propertyName: "secret"] = ScopedServiceTokenTestData.ServiceClientSecret,
                [propertyName: "standardFlowEnabled"] = false,
                [propertyName: "directAccessGrantsEnabled"] = false,
                [propertyName: "serviceAccountsEnabled"] = true,
                [propertyName: "fullScopeAllowed"] = false,
                [propertyName: "defaultClientScopes"] = new JsonArray(
                    ServiceClientDefaultScopes.Select(scope => (JsonNode)JsonValue.Create(scope)!).ToArray()),
            });
        await Realm.AssignRealmRoleAsync(await Realm.GetServiceAccountUserIdAsync(serviceUuid), serviceRole);

        await AddDownstreamScopeAsync(serviceUuid, coreUuid, ScopedServiceTokenTestData.FirstScope, first);
        await AddDownstreamScopeAsync(serviceUuid, coreUuid, ScopedServiceTokenTestData.SecondScope, second);
    }

    private async Task AddDownstreamScopeAsync(string serviceUuid, string coreUuid, string scopeName, JsonObject role)
    {
        var scopeId = await Realm.CreateClientScopeAsync(scopeName);
        await Realm.MapClientRoleToScopeAsync(scopeId, coreUuid, role);
        await Realm.AddClientScopeAsync(serviceUuid, scopeId, isDefault: false);
    }
}
