using System.Security.Claims;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
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
public static class ActorClaimsReader
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
    /// than one organization. Also thrown when a service token comes from a client
    /// outside <see cref="ServiceActorOptions.AllowedClients"/>, or has no valid actor header.
    /// </exception>
    public static ActorContext Read(ClaimsPrincipal user, IHeaderDictionary headers, ServiceActorOptions options) =>
        IsServiceToken(user) ? ReadServiceActor(user, headers, options) : ReadUser(user);

    /// <summary>
    /// Reads the company of a user token. The <c>organization</c> claim must occur exactly once with a GUID value.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="companyId">The company when the token holds exactly one valid organization.</param>
    /// <returns><see langword="true"/> when the token names exactly one company.</returns>
    public static bool TryReadCompanyId(ClaimsPrincipal user, out Guid companyId)
    {
        var organizations = user.FindAll(KeycloakClaimTypes.Organization)
            .Select(claim => claim.Value)
            .ToList();

        if (organizations.Count == 1 && Guid.TryParse(organizations[0], out companyId))
        {
            return true;
        }

        companyId = Guid.Empty;

        return false;
    }

    /// <summary>Reads the user ID of a token: the <c>sub</c> claim, when it is a GUID.</summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="userId">The user ID, or <see cref="Guid.Empty"/> when the claim is missing or not a GUID.</param>
    /// <returns><see langword="true"/> when the token has a GUID <c>sub</c> claim.</returns>
    public static bool TryReadUserId(ClaimsPrincipal user, out Guid userId)
    {
        if (Guid.TryParse(user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out userId))
        {
            return true;
        }

        userId = Guid.Empty;

        return false;
    }

    /// <summary>
    /// Reads the actor of a user token, with no service path. A connection that always has a user token (a hub
    /// connection) uses it.
    /// </summary>
    /// <param name="user">The authenticated user.</param>
    /// <returns>The user, the company, and the employee of the token.</returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the token has no valid <c>sub</c>, <c>organization</c>, or <c>employee_id</c>.
    /// </exception>
    public static ActorContext ReadUser(ClaimsPrincipal user)
    {
        if (!TryReadUserId(user, out var userId))
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
        };
    }

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

    private static Guid ReadCompanyId(ClaimsPrincipal user)
        => TryReadCompanyId(user, out var companyId) ? companyId : throw CurrentUserErrors.OrganizationInvalid();
}
