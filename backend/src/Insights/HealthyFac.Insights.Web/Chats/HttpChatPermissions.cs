using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Shared.Web.Authentication;

namespace HealthyFac.Insights.Web.Chats;

/// <summary>
/// Reads the permission claims of the user of the current request. The chat offers only the tools whose permissions
/// the token holds.
/// </summary>
/// <param name="httpContextAccessor">Gives the context of the current request.</param>
public sealed class HttpChatPermissions(IHttpContextAccessor httpContextAccessor) : IChatPermissions
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public IReadOnlyCollection<string> Read() =>
        _httpContextAccessor.HttpContext?.User.FindAll(KeycloakClaimTypes.Permissions)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal)
        ?? [];
}
