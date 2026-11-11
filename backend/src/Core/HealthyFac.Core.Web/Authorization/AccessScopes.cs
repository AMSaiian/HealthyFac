using System.Security.Claims;
using HealthyFac.Core.Application.Authorization;
using HealthyFac.Shared.Web.Authentication;

namespace HealthyFac.Core.Web.Authorization;

/// <summary>
/// Resolves the <see cref="AccessScope"/> of a request from the permissions of the token.
/// </summary>
public static class AccessScopes
{
    /// <summary>
    /// Resolves the scope: <see cref="AccessScope.Company"/> when the token holds the manage permission of the feature,
    /// else <see cref="AccessScope.Own"/>.
    /// </summary>
    /// <param name="user">The user of the request.</param>
    /// <param name="managePermission">The manage permission of the feature.</param>
    /// <returns>The scope of the actor.</returns>
    public static AccessScope Resolve(ClaimsPrincipal user, string managePermission) =>
        user.HasClaim(KeycloakClaimTypes.Permissions, managePermission) ? AccessScope.Company : AccessScope.Own;
}
