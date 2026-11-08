using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.UnitTests.CurrentUser.Data;
using Microsoft.AspNetCore.TestHost;

namespace HealthyFac.Shared.UnitTests.CurrentUser;

public sealed class ActorContextMiddlewareTests
{
    [Fact]
    public async Task Request_WithoutUser_RunsAsAnonymous()
    {
        using var host = await ActorHostFactory.StartAsync();
        using var client = host.GetTestClient();

        var userId = await client.GetStringAsync(ActorHostFactory.UserIdPath, TestContext.Current.CancellationToken);

        userId.Should().Be(KnownActorsTestData.AnonymousId.ToString());
    }

    [Fact]
    public async Task Request_WithAuthenticatedUser_RunsAsUserOfTheToken()
    {
        using var host = await ActorHostFactory.StartAsync(ActorClaimsTestData.ValidPrincipal);
        using var client = host.GetTestClient();

        var userId = await client.GetStringAsync(ActorHostFactory.UserIdPath, TestContext.Current.CancellationToken);

        userId.Should().Be(ActorTestData.UserId.ToString());
    }

    [Fact]
    public async Task Request_WithAllowedServiceToken_RunsAsActorOfTheHeader()
    {
        using var host = await ActorHostFactory.StartAsync(
            IncomingTokenTestData.ServicePrincipal,
            ActorClaimsTestData.AllowedServiceClients);
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, ActorHostFactory.UserIdPath);
        request.Headers.Add(ActorHeaderNames.ActorId, ActorTestData.UserId.ToString());

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        var userId = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        userId.Should().Be(ActorTestData.UserId.ToString());
    }

    [Fact]
    public async Task Request_WithServiceTokenOfClientNotAllowed_Fails()
    {
        using var host = await ActorHostFactory.StartAsync(IncomingTokenTestData.ServicePrincipal);
        using var client = host.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, ActorHostFactory.UserIdPath);
        request.Headers.Add(ActorHeaderNames.ActorId, ActorTestData.UserId.ToString());

        var act = () => client.SendAsync(request);

        await act.Should().ThrowAsync<Application.ErrorHandling.UnauthorizedAccessException>();
    }
}
