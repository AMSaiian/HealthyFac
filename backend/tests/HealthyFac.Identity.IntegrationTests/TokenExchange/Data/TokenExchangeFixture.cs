using System.Text.Json.Nodes;
using HealthyFac.Identity.IntegrationTests.Realm.Data;
using HealthyFac.Testing.Containers;

namespace HealthyFac.Identity.IntegrationTests.TokenExchange.Data;

/// <summary>
/// Starts the realm and creates the temporary target client of the exchange tests: a client with one role, and an
/// optional client scope on <c>healthyfac-core</c> that adds the target audience. It also starts Redis, because the
/// token store of <c>AddServiceTokens</c> keeps the exchanged tokens there.
/// </summary>
public sealed class TokenExchangeFixture : IAsyncLifetime
{
    public KeycloakRealmFixture Realm { get; } = new();

    public RedisTestServer Redis { get; } = new();

    public async Task InitializeAsync()
    {
        await Redis.StartAsync();
        await Realm.InitializeAsync();

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
        await Realm.AddClientScopeAsync(coreUuid, scopeId, false);
    }

    public async Task DisposeAsync()
    {
        await Realm.DisposeAsync();
        await Redis.DisposeAsync();
    }
}
