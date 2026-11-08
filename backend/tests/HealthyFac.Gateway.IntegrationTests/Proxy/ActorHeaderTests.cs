using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Proxy.Data;
using HealthyFac.Shared.Infrastructure.CurrentUser;

namespace HealthyFac.Gateway.IntegrationTests.Proxy;

[Collection(GatewayProxyCollection.Name)]
public sealed class ActorHeaderTests(GatewayEnvironment environment) : IAsyncLifetime
{
    private readonly GatewayEnvironment _environment = environment;

    private readonly EchoServer _core = new();

    public async ValueTask InitializeAsync() => await _core.StartAsync();

    public ValueTask DisposeAsync() => _core.DisposeAsync();

    [Fact]
    public async Task Gateway_WithActorHeadersFromClient_RemovesThem()
    {
        await using var gateway = _environment.CreateGateway(_core);
        using var client = gateway.CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Add(ActorHeaderNames.ActorId, ActorHeaderTestData.ForgedActorId);
        client.DefaultRequestHeaders.Add(ActorHeaderNames.CompanyId, ActorHeaderTestData.ForgedCompanyId);
        client.DefaultRequestHeaders.Add(ActorHeaderTestData.KeptHeader, ActorHeaderTestData.KeptValue);

        using var response = await client.GetAsync(
            ActorHeaderTestData.VersionedPath, TestContext.Current.CancellationToken);

        var forwardedRequest = _core.Requests.Single();

        forwardedRequest.Headers.Should().ContainKey(ActorHeaderTestData.KeptHeader);
        forwardedRequest.Headers.Should().NotContainKey(ActorHeaderNames.ActorId);
        forwardedRequest.Headers.Should().NotContainKey(ActorHeaderNames.CompanyId);
    }
}
