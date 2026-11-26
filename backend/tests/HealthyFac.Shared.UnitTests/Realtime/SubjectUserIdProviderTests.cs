using HealthyFac.Shared.UnitTests.Realtime.Data;
using HealthyFac.Shared.Web.Realtime;
using HealthyFac.Testing.Features;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

namespace HealthyFac.Shared.UnitTests.Realtime;

[Feature(TestFeatures.Realtime)]
public sealed class SubjectUserIdProviderTests
{
    [Fact]
    public void GetUserId_WithAUserToken_ReturnsTheUserIdOfTheActor()
    {
        var connection = CreateConnection(
            RealtimeHubTestData.CreateUser(RealtimeHubTestData.WorkerPermissions, RealtimeHubTestData.CompanyText));

        var userId = new SubjectUserIdProvider().GetUserId(connection);

        userId.Should().Be(RealtimeHubTestData.UserId.ToString());
    }

    [Fact]
    public void GetUserId_ThenTheHubReadsTheActor_BuildsTheActorOnce()
    {
        var connection = CreateConnection(
            RealtimeHubTestData.CreateUser(RealtimeHubTestData.WorkerPermissions, RealtimeHubTestData.CompanyText));

        new SubjectUserIdProvider().GetUserId(connection);

        var actor = ConnectionActor.Of(connection.Items, new System.Security.Claims.ClaimsPrincipal());

        actor.UserId.Should().Be(RealtimeHubTestData.UserId);
    }

    private static HubConnectionContext CreateConnection(System.Security.Claims.ClaimsPrincipal user) =>
        new(
            new DefaultConnectionContext { User = user }, new HubConnectionContextOptions(),
            NullLoggerFactory.Instance);
}
