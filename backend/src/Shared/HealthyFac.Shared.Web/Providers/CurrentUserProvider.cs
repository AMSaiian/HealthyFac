using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using HealthyFac.Shared.Application.Interfaces;
using UnauthorizedAccessException = HealthyFac.Shared.Application.ErrorHandling.UnauthorizedAccessException;

namespace HealthyFac.Shared.Web.Providers;

/// <summary>
/// Resolves the current authenticated user from HTTP context claims.
/// </summary>
public class CurrentUserProvider(
    IHttpContextAccessor httpContextAccessor,
    ILogger<CurrentUserProvider> logger
) : ICurrentUserProvider
{
    /// <summary>
    /// Attempts to retrieve the current user identifier from HTTP claims without throwing exceptions.
    /// </summary>
    /// <returns>The user <see cref="Guid"/> identifier when authenticated; otherwise, <see langword="null"/>.</returns>
    public Guid? TryGetUserId()
    {
        try
        {
            var userIdClaim =
                httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypes.NameIdentifier)
                    ?.Value
                ?? httpContextAccessor.HttpContext?.User?
                    .FindFirst(ClaimTypeConstants.Subject)
                    ?.Value;

            if (Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            return null;
        }
        catch (Exception exception)
        {
            logger.LogInformation(exception, CurrentUserConstants.FailedToResolveUserIdMessage);
            return null;
        }
    }

    /// <summary>
    /// Retrieves the current user identifier or throws <see cref="UnauthorizedAccessException"/> when not
    /// authenticated.
    /// </summary>
    /// <returns>The authenticated user <see cref="Guid"/> identifier.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the user is not authenticated or the identifier cannot
    /// be resolved.</exception>
    public Guid GetUserId()
        => TryGetUserId()
            ?? throw new UnauthorizedAccessException(
                CurrentUserConstants.UserNotAuthenticatedMessage);
}
