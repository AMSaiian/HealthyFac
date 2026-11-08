using System.Net;
using System.Net.Http.Headers;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.UnitTests.Authentication.Data;
using HealthyFac.Shared.UnitTests.CurrentUser.Data;
using HealthyFac.Testing.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Shared.UnitTests.Authentication;

public sealed class ServiceTokenEndpointTests
{
    private const string BearerScheme = "Bearer";

    private readonly TestTokenFactory _tokens = new();

    [Fact]
    public async Task ServiceToken_WithPermissionAndActorHeader_ReachesEndpointAsTheHeaderActor()
    {
        using var host = await ServiceTokenHostFactory.StartAsync(_tokens);
        using var client = CreateClient(host, [ServiceTokenHostFactory.InvitePermission]);

        using var response = await client.GetAsync(
            ServiceTokenHostFactory.InviteRoute, TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Be(ActorTestData.UserId.ToString());
    }

    [Fact]
    public async Task ServiceToken_WithoutPermission_Returns403()
    {
        using var host = await ServiceTokenHostFactory.StartAsync(_tokens);
        using var client = CreateClient(host, []);

        using var response = await client.GetAsync(
            ServiceTokenHostFactory.InviteRoute, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private HttpClient CreateClient(IHost host, IReadOnlyList<string> permissions)
    {
        var client = host.GetTestClient();
        var token = _tokens.CreateServiceToken(new TestServiceTokenClaims { Permissions = permissions });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(BearerScheme, token);
        client.DefaultRequestHeaders.Add(ActorHeaderNames.ActorId, ActorTestData.UserId.ToString());
        client.DefaultRequestHeaders.Add(ActorHeaderNames.CompanyId, ActorTestData.CompanyId.ToString());

        return client;
    }
}
