using System.Net.Http.Headers;
using HealthyFac.Shared.Application.CurrentUser;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace HealthyFac.Shared.Web.CurrentUser;

/// <summary>
/// Reads the bearer token of the current request through <see cref="IHttpContextAccessor"/>, whose context flows
/// with the request. A handler of <c>IHttpClientFactory</c> lives in its own DI scope, so a scoped service cannot
/// give it the request.
/// </summary>
/// <param name="httpContextAccessor">Gives the context of the current request.</param>
internal sealed class HttpIncomingAccessTokenAccessor(IHttpContextAccessor httpContextAccessor)
    : IIncomingAccessTokenAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public string? TryGetUserAccessToken()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null || !UserTokenShape.Matches(context.User))
        {
            return null;
        }

        var isBearer = AuthenticationHeaderValue.TryParse(context.Request.Headers.Authorization, out var header)
            && header.Scheme.Equals(JwtBearerDefaults.AuthenticationScheme, StringComparison.OrdinalIgnoreCase);

        return isBearer ? header!.Parameter : null;
    }

    public Guid? TryGetUserId()
    {
        var subject = _httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return TryGetUserAccessToken() is not null
            && Guid.TryParse(subject, out var userId)
                ? userId
                : null;
    }
}
