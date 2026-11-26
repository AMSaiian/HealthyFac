using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Realtime.Data;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Testing.Authentication;
using HealthyFac.Testing.Features;

namespace HealthyFac.Gateway.IntegrationTests.Realtime;

[Collection(GatewayProxyCollection.Name)]
[Feature(TestFeatures.Gateway)]
[Feature(TestFeatures.Realtime)]
public sealed class RealtimeBackplaneTests(GatewayEnvironment environment) : IAsyncLifetime
{
    private readonly GatewayEnvironment _environment = environment;

    private readonly EchoServer _core = new(useForwardedHeaders: true);

    private RealtimePusherHost _pusher = null!;

    public async ValueTask InitializeAsync()
    {
        await _core.StartAsync();
        _pusher = await RealtimePusherHost.StartAsync(_environment.RedisConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        await _pusher.DisposeAsync();
        await _core.DisposeAsync();
    }

    [Fact]
    public async Task Push_ToUserFromHostWithoutHub_ReachesTheGatewayConnectionWithEncryptedIds()
    {
        var subject = Guid.NewGuid();
        await using var gateway = _environment.CreateGateway(_core);

        await using var client = await RealtimeTestClient.ConnectAsync(
            gateway,
            gateway.Tokens.CreateToken(RealtimeTestData.Claims(subject)));

        var received = await client.ReceiveNotificationAfterPushingAsync(() => PushToUserAsync(subject));

        received.Should().NotBeNull();
        received.Value.GetProperty(propertyName: "kind").GetString().Should().Be(expected: "workOrderAssigned");

        var machineId = received.Value.GetProperty(propertyName: "data")
            .GetProperty(propertyName: "machineId")
            .GetString();

        machineId.Should().StartWith(expected: "v1_").And.NotContain(RealtimeTestData.MachineId.ToString());
    }

    [Fact]
    public async Task Push_ToTopicGroup_ReachesOnlyHolderOfThePermissionInTheCompany()
    {
        var group = RealtimeGroups.ForTopic(TestTokenClaims.DefaultCompanyId, RealtimeTopic.Cases);
        await using var gateway = _environment.CreateGateway(_core);

        await using var allowed = await RealtimeTestClient.ConnectAsync(
            gateway,
            gateway.Tokens.CreateToken(RealtimeTestData.Claims(Guid.NewGuid())));

        await using var withoutPermission = await RealtimeTestClient.ConnectAsync(
            gateway,
            gateway.Tokens.CreateToken(RealtimeTestData.Claims(Guid.NewGuid(), RealtimeTestData.MachinesReader)));

        await using var otherCompany = await RealtimeTestClient.ConnectAsync(
            gateway,
            gateway.Tokens.CreateToken(
                RealtimeTestData.Claims(Guid.NewGuid(), companyId: RealtimeTestData.OtherCompanyId)));

        var hint = await allowed.ReceiveHintAfterPushingAsync(() =>
            _pusher.Hub.Clients.Group(group).Hint(new RealtimeHint { Topic = RealtimeTopic.Cases }));

        var missedByWithoutPermission = await withoutPermission.ReceiveHintAsync(RealtimeTestClient.SilenceTimeout);
        var missedByOtherCompany = await otherCompany.ReceiveHintAsync(RealtimeTestClient.SilenceTimeout);

        hint.Should().NotBeNull();
        hint.Value.GetProperty(propertyName: "topic").GetString().Should().Be(expected: "cases");
        missedByWithoutPermission.Should().BeNull();
        missedByOtherCompany.Should().BeNull();
    }

    [Fact]
    public async Task Push_ToUserConnectedToTwoGateways_ReachesBothThroughTheBackplane()
    {
        var subject = Guid.NewGuid();
        await using var firstGateway = _environment.CreateGateway(_core);
        await using var secondGateway = _environment.CreateGateway(_core);

        await using var first = await RealtimeTestClient.ConnectAsync(
            firstGateway,
            firstGateway.Tokens.CreateToken(RealtimeTestData.Claims(subject)));

        await using var second = await RealtimeTestClient.ConnectAsync(
            secondGateway,
            secondGateway.Tokens.CreateToken(RealtimeTestData.Claims(subject)));

        var receivedByFirst = await first.ReceiveNotificationAfterPushingAsync(() => PushToUserAsync(subject));
        var receivedBySecond = await second.ReceiveNotificationAfterPushingAsync(() => PushToUserAsync(subject));

        receivedByFirst.Should().NotBeNull();
        receivedBySecond.Should().NotBeNull();
    }

    private Task PushToUserAsync(Guid subject) =>
        _pusher.Hub.Clients.User(subject.ToString()).Notification(RealtimeTestData.CreateNotification(subject));
}
