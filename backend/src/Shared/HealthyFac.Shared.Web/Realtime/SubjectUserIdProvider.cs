using Microsoft.AspNetCore.SignalR;

namespace HealthyFac.Shared.Web.Realtime;

/// <summary>
/// Names the user of a hub connection by the user ID of its actor. A push to one user uses that value. A token
/// without a valid actor ends the connect.
/// </summary>
public sealed class SubjectUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
        => ConnectionActor.Of(connection.Items, connection.User).UserId.ToString();
}
