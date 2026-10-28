using System.Security.Claims;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Web.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;
using UnauthorizedAccessException = HealthyFac.Shared.Application.ErrorHandling.UnauthorizedAccessException;

namespace HealthyFac.Shared.Web.CurrentUser;

/// <summary>
/// Builds the actor of a request from the claims of a validated access token.
/// </summary>
internal static class ActorClaimsReader
{
    /// <summary>
    /// Reads the user, the company, and the employee from the claims. The token handler splits a JSON array
    /// claim into repeated claims. The <c>organization</c> claim must occur exactly once with a GUID value,
    /// because two organizations make the tenant ambiguous. A JSON value is not a GUID, so it fails.
    /// </summary>
    /// <param name="user">The authenticated user of the request.</param>
    /// <returns>The actor of the request.</returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when <c>sub</c>, <c>organization</c>, or <c>employee_id</c> is missing or is not a GUID, or when
    /// the token holds more than one organization.
    /// </exception>
    public static ActorContext Read(ClaimsPrincipal user)
    {
        if (!Guid.TryParse(user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId))
        {
            throw CurrentUserErrors.SubjectInvalid();
        }

        if (!Guid.TryParse(user.FindFirst(KeycloakClaimTypes.EmployeeId)?.Value, out var employeeId))
        {
            throw CurrentUserErrors.EmployeeIdInvalid();
        }

        return new ActorContext
        {
            UserId = userId,
            CompanyId = ReadCompanyId(user),
            EmployeeId = employeeId,
        };
    }

    private static Guid ReadCompanyId(ClaimsPrincipal user)
    {
        var organizations = user.FindAll(KeycloakClaimTypes.Organization)
            .Select(claim => claim.Value)
            .ToList();

        if (organizations.Count != 1 || !Guid.TryParse(organizations[0], out var companyId))
        {
            throw CurrentUserErrors.OrganizationInvalid();
        }

        return companyId;
    }
}
