using System.Collections.Frozen;
using System.Security.Claims;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Web.Authentication;
using HealthyFac.Shared.Web.CurrentUser.ServiceActors;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using UnauthorizedAccessException = HealthyFac.Shared.Application.ErrorHandling.UnauthorizedAccessException;

namespace HealthyFac.Shared.Web.CurrentUser;

/// <summary>
/// Builds the actor of a request from the claims of a validated access token. A user token gives the user. A
/// service token (Client Credentials) gives the actor that the calling service sends in the actor headers.
/// </summary>
internal static class ActorClaimsReader
{
    /// <summary>
    /// Reads the actor from a token of the user shape or of the service shape. A token with the client ID claim and
    /// neither the <c>organization</c> claim nor the <c>employee_id</c> claim has the service shape. Any other token
    /// has the user shape, so an exchanged token (which also has a client ID claim) is a user token and ignores the
    /// actor headers. A user token gives the user, the company, and the employee. The token handler splits a JSON
    /// array claim into repeated claims. The <c>organization</c> claim must occur exactly once with a GUID value,
    /// because two organizations make the tenant ambiguous. A JSON value is not a GUID, so it fails. A service token
    /// of an allowed client runs as the actor that the headers name, with the same trust as a message. The restored
    /// actor gets no rights of its own: the permissions come from the token.
    /// </summary>
    /// <param name="user">The authenticated user of the request.</param>
    /// <param name="headers">The request headers, which carry the actor of a service call.</param>
    /// <param name="options">Lists the clients whose service tokens the service accepts.</param>
    /// <returns>The actor of the request.</returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when a user token has no valid <c>sub</c>, <c>organization</c>, or <c>employee_id</c>, or holds more
    /// than one organization, or has a <c>department_ids</c> value that is not a GUID. A missing
    /// <c>department_ids</c> claim gives an actor with no departments. Also thrown when a service token comes from a
    /// client outside <see cref="ServiceActorOptions.AllowedClients"/>, or has no valid actor header.
    /// </exception>
    public static ActorContext Read(ClaimsPrincipal user, IHeaderDictionary headers, ServiceActorOptions options) =>
        IsServiceToken(user) ? ReadServiceActor(user, headers, options) : ReadUser(user);

    private static bool IsServiceToken(ClaimsPrincipal user) =>
        user.HasClaim(claim => claim.Type == KeycloakClaimTypes.ClientId)
        && !user.HasClaim(claim => claim.Type == KeycloakClaimTypes.Organization)
        && !user.HasClaim(claim => claim.Type == KeycloakClaimTypes.EmployeeId);

    private static ActorContext ReadServiceActor(
        ClaimsPrincipal user,
        IHeaderDictionary headers,
        ServiceActorOptions options
    )
    {
        var clientId = user.FindFirst(KeycloakClaimTypes.ClientId)!.Value;
        if (!options.AllowedClients.Contains(clientId, StringComparer.Ordinal))
        {
            throw CurrentUserErrors.ServiceClientNotAllowed(clientId);
        }

        var isActorValid = Guid.TryParse(headers[ActorHeaderNames.ActorId].ToString(), out var actorId);
        if (!isActorValid || actorId == Guid.Empty)
        {
            throw CurrentUserErrors.ServiceActorMissing();
        }

        return KnownActors.Restore(actorId, ReadCompanyHeader(headers));
    }

    private static Guid? ReadCompanyHeader(IHeaderDictionary headers)
    {
        if (!headers.TryGetValue(ActorHeaderNames.CompanyId, out var rawCompanyId))
        {
            return null;
        }

        return Guid.TryParse(rawCompanyId.ToString(), out var companyId)
            ? companyId
            : throw CurrentUserErrors.ServiceActorMissing();
    }

    private static ActorContext ReadUser(ClaimsPrincipal user)
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
            Kind = ActorKind.User,
            UserId = userId,
            CompanyId = ReadCompanyId(user),
            EmployeeId = employeeId,
            DepartmentIds = ReadDepartmentIds(user),
        };
    }

    private static FrozenSet<Guid> ReadDepartmentIds(ClaimsPrincipal user) =>
        user.FindAll(KeycloakClaimTypes.DepartmentIds)
            .Select(claim => Guid.TryParse(claim.Value, out var departmentId)
                ? departmentId
                : throw CurrentUserErrors.DepartmentIdsInvalid())
            .ToFrozenSet();

    private static Guid ReadCompanyId(ClaimsPrincipal user)
    {
        var organizations = user.FindAll(KeycloakClaimTypes.Organization)
            .Select(claim => claim.Value)
            .ToList();

        if (organizations.Count != 1 || !Guid.TryParse(organizations[index: 0], out var companyId))
        {
            throw CurrentUserErrors.OrganizationInvalid();
        }

        return companyId;
    }
}
