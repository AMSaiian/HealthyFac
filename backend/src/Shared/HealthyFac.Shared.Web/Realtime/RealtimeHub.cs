using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Web.Authentication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace HealthyFac.Shared.Web.Realtime;

/// <summary>
/// The hub of the real-time push. It only joins a connection to the groups that the permissions of its token allow
/// (<see cref="RealtimeAudiences"/>), and it has no method that a client calls.
/// </summary>
public sealed class RealtimeHub(ILogger<RealtimeHub> logger) : Hub<IRealtimeClient>
{
    private readonly ILogger<RealtimeHub> _logger = logger;

    /// <summary>
    /// Joins the connection to the groups that its permissions allow.
    /// </summary>
    /// <remarks>
    /// A group join subscribes to the Redis channel of the group. When a join fails after the connect, the method
    /// logs a warning and keeps the connection open without the remaining groups. A throw closes the connection with
    /// <c>allowReconnect: false</c>, so the automatic reconnect of the client does not run, and the connection waits
    /// for the web app to start a new one. While Redis is down, the SignalR
    /// <c>RedisHubLifetimeManager.OnConnectedAsync</c> fails the connect before the hub runs, and the web app starts a
    /// new connection.
    /// </remarks>
    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (user is null)
        {
            await base.OnConnectedAsync();

            return;
        }

        var companyId = ConnectionActor.Of(Context.Items, user).CompanyId!.Value;

        var permissions = user.FindAll(KeycloakClaimTypes.Permissions)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal);

        try
        {
            foreach (var (kind, permission) in RealtimeAudiences.KindPermissions)
            {
                if (permissions.Contains(permission))
                {
                    await Groups.AddToGroupAsync(
                        Context.ConnectionId,
                        RealtimeGroups.ForKind(companyId, kind),
                        Context.ConnectionAborted);
                }
            }

            foreach (var (topic, permission) in RealtimeAudiences.TopicPermissions)
            {
                if (permissions.Contains(permission))
                {
                    await Groups.AddToGroupAsync(
                        Context.ConnectionId,
                        RealtimeGroups.ForTopic(companyId, topic),
                        Context.ConnectionAborted);
                }
            }
        }
        catch (Exception exception) when (
            exception is RedisException or RedisTimeoutException
            && !Context.ConnectionAborted.IsCancellationRequested)
        {
            _logger.LogWarning(
                exception,
                message: "Joining the real-time groups of connection {ConnectionId} failed, the connection stays open",
                Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }
}
