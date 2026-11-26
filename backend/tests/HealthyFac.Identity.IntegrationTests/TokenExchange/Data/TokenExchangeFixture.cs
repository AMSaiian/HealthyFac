using System.Text.Json.Nodes;
using HealthyFac.Identity.IntegrationTests.Realm.Data;
using HealthyFac.Testing.Containers;

namespace HealthyFac.Identity.IntegrationTests.TokenExchange.Data;

/// <summary>
/// Creates the temporary target client of the exchange tests in the started realm: a client with one role, and an
/// optional client scope on <c>healthyfac-core</c> that adds the target audience. It also holds the Redis server,
/// because the token store of <c>AddServiceTokens</c> keeps the exchanged tokens there.
/// </summary>
public sealed class TokenExchangeFixture
{
    public TokenExchangeFixture(KeycloakRealmFixture realm, RedisTestServer redis)
    {
        Realm = realm;
        Redis = redis;
    }

    public KeycloakRealmFixture Realm { get; }

    public RedisTestServer Redis { get; }

    public async ValueTask InitializeAsync()
    {
        var targetUuid = await Realm.CreateClientAsync(
            new JsonObject
            {
                ["clientId"] = TokenExchangeTestData.TargetClientId,
                ["enabled"] = true,
                ["publicClient"] = false,
            });

        await Realm.CreateClientRoleAsync(targetUuid, TokenExchangeTestData.TargetRoleName);

        var scopeId = await Realm.CreateClientScopeAsync(
            TokenExchangeTestData.TargetScopeName,
            new JsonObject
            {
                ["name"] = $"{TokenExchangeTestData.TargetClientId}-audience",
                ["protocol"] = "openid-connect",
                ["protocolMapper"] = "oidc-audience-mapper",
                ["config"] = new JsonObject
                {
                    ["included.client.audience"] = TokenExchangeTestData.TargetClientId,
                    ["access.token.claim"] = "true",
                    ["id.token.claim"] = "false",
                    ["introspection.token.claim"] = "true",
                },
            });

        var coreUuid = await Realm.FindClientUuidAsync(RealmTestData.CoreClientId);
        await Realm.AddClientScopeAsync(coreUuid, scopeId, isDefault: false);
    }
}
