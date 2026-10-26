using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.UnitTests.CurrentUser.Data;
using Microsoft.AspNetCore.TestHost;

namespace HealthyFac.Shared.UnitTests.CurrentUser;

public sealed class ActorContextMiddlewareTests
{
    [Fact]
    public async Task Request_WithActorMiddleware_RunsAsSystemActor()
    {
        using var host = await ActorHostFactory.StartAsync();
        using var client = host.GetTestClient();

        var userId = await client.GetStringAsync(ActorHostFactory.UserIdPath);

        userId.Should().Be(SystemActor.Id.ToString());
    }
}
