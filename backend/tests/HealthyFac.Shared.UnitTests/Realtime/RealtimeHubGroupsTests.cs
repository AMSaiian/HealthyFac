using System.Security.Claims;
using HealthyFac.Shared.UnitTests.Realtime.Data;
using HealthyFac.Shared.Web.Realtime;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using StackExchange.Redis;

namespace HealthyFac.Shared.UnitTests.Realtime;

public sealed class RealtimeHubGroupsTests
{
    [Fact]
    public async Task OnConnectedAsync_WithEveryPermission_JoinsTheNineGroups()
    {
        var groups = new RecordingGroupManager();
        var hub = CreateHub(groups, RealtimeHubTestData.SupervisorPermissions, RealtimeHubTestData.CompanyText);

        await hub.OnConnectedAsync();

        groups.JoinedGroups.Should().BeEquivalentTo(RealtimeHubTestData.SupervisorGroups);
        groups.JoinedGroups.Should().HaveCount(expected: 9);
    }

    [Fact]
    public async Task OnConnectedAsync_WithoutManagePermissions_JoinsOnlyTheSevenGroups()
    {
        var groups = new RecordingGroupManager();
        var hub = CreateHub(groups, RealtimeHubTestData.WorkerPermissions, RealtimeHubTestData.CompanyText);

        await hub.OnConnectedAsync();

        groups.JoinedGroups.Should().BeEquivalentTo(RealtimeHubTestData.WorkerGroups);
        groups.JoinedGroups.Should().HaveCount(expected: 7);
    }

    [Fact]
    public async Task OnConnectedAsync_WithPermissionOutsideTheMap_AddsNoGroup()
    {
        var groups = new RecordingGroupManager();
        var hub = CreateHub(groups, RealtimeHubTestData.WorkerExtraPermissions, RealtimeHubTestData.CompanyText);

        await hub.OnConnectedAsync();

        groups.JoinedGroups.Should().BeEquivalentTo(RealtimeHubTestData.WorkerGroups);
    }

    [Theory]
    [MemberData(nameof(RealtimeHubTestData.IncompleteUsers), MemberType = typeof(RealtimeHubTestData))]
    public async Task OnConnectedAsync_WithATokenWithoutOrganizationOrEmployee_EndsTheConnectAndJoinsNoGroup(
        string? organization,
        bool hasEmployee
    )
    {
        var groups = new RecordingGroupManager();
        var hub = CreateHub(groups, RealtimeHubTestData.SupervisorPermissions, organization, hasEmployee: hasEmployee);

        var act = async () => await hub.OnConnectedAsync();

        await act.Should().ThrowAsync<HealthyFac.Shared.Application.ErrorHandling.UnauthorizedAccessException>();
        groups.JoinedGroups.Should().BeEmpty();
    }

    [Fact]
    public async Task OnConnectedAsync_WhenRedisFailsOnAJoin_KeepsTheConnectionAndLogsAWarning()
    {
        var logger = new FakeLogger<RealtimeHub>();
        var groups = new ThrowingGroupManager(
            new RedisConnectionException(
                ConnectionFailureType.UnableToConnect,
                CommandFlags.None,
                message: "down",
                innerException: null,
                CommandStatus.Unknown));
        var hub = CreateHub(groups, RealtimeHubTestData.SupervisorPermissions, RealtimeHubTestData.CompanyText, logger);

        await hub.OnConnectedAsync();

        groups.JoinAttempts.Should().Be(expected: 1);
        logger.Collector.GetSnapshot().Should().ContainSingle(record => record.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task OnConnectedAsync_WhenAJoinFailsWithAnotherException_Propagates()
    {
        var groups = new ThrowingGroupManager(new InvalidOperationException());
        var hub = CreateHub(groups, RealtimeHubTestData.SupervisorPermissions, RealtimeHubTestData.CompanyText);

        var act = async () => await hub.OnConnectedAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static RealtimeHub CreateHub(
        IGroupManager groups,
        IReadOnlyList<string> permissions,
        string? organization,
        ILogger<RealtimeHub>? logger = null,
        bool hasEmployee = true
    )
    {
        var user = RealtimeHubTestData.CreateUser(permissions, organization, hasEmployee);

        return new RealtimeHub(logger ?? new FakeLogger<RealtimeHub>())
        {
            Groups = groups,
            Context = new FakeHubCallerContext(user),
        };
    }

    private sealed class RecordingGroupManager : IGroupManager
    {
        public List<string> JoinedGroups { get; } = [];

        public Task AddToGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            JoinedGroups.Add(groupName);

            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => Task.CompletedTask;
    }

    private sealed class ThrowingGroupManager(Exception exception) : IGroupManager
    {
        private readonly Exception _exception = exception;

        public int JoinAttempts { get; private set; }

        public Task AddToGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            JoinAttempts++;

            return Task.FromException(_exception);
        }

        public Task RemoveFromGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default(CancellationToken)
        ) => Task.CompletedTask;
    }

    private sealed class FakeHubCallerContext(ClaimsPrincipal user) : HubCallerContext
    {
        public override string ConnectionId => "connection-1";

        public override string? UserIdentifier => null;

        public override ClaimsPrincipal? User { get; } = user;

        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();

        public override IFeatureCollection Features { get; } = new FeatureCollection();

        public override CancellationToken ConnectionAborted => CancellationToken.None;

        public override void Abort()
        {
        }
    }
}
