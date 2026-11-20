using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace HealthyFac.Shared.Web.Realtime;

/// <summary>
/// Names the user of a hub connection by the <c>sub</c> claim of the token. A push to one user uses that value.
/// </summary>
public sealed class SubjectUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
        => connection.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
}
