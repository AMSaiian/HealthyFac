using System.Security.Claims;
using HealthyFac.Shared.Web.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;

namespace HealthyFac.Shared.Web.CurrentUser;

/// <summary>
/// Defines which claims make an access token a user token. A service token (Client Credentials) has none of the
/// user claims, so the shape tells the two apart.
/// </summary>
internal static class UserTokenShape
{
    /// <summary>
    /// Checks that the principal has <c>sub</c>, exactly one <c>organization</c>, and <c>employee_id</c>. It checks
    /// the presence of the claims only: the actor middleware checks their values.
    /// </summary>
    /// <param name="user">The authenticated principal of a request.</param>
    /// <returns><see langword="true"/> for a user token; otherwise, <see langword="false"/>.</returns>
    public static bool Matches(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true
        && user.HasClaim(claim => claim.Type == JwtRegisteredClaimNames.Sub)
        && user.HasClaim(claim => claim.Type == KeycloakClaimTypes.EmployeeId)
        && user.FindAll(KeycloakClaimTypes.Organization).Count() == 1;
}
