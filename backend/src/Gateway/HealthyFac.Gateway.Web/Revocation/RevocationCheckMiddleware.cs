using HealthyFac.Gateway.Web.Proxy;
using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Shared.Infrastructure.Revocation;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Net.Http.Headers;
using StackExchange.Redis;

namespace HealthyFac.Gateway.Web.Revocation;

/// <summary>
/// Rejects a request when its token was issued before the revocation time of the user. Redis is not a hard
/// dependency: when the circuit breaker is open or Redis fails, the request passes, and the breaker logs the state
/// change. Run the middleware after authentication.
/// </summary>
/// <param name="store">The revocation list.</param>
/// <param name="breaker">The shared breaker that protects the gateway from a slow Redis.</param>
public sealed class RevocationCheckMiddleware(
    ITokenRevocationStore store,
    RedisCircuitBreaker breaker
) : IMiddleware
{
    private readonly ITokenRevocationStore _store = store;
    private readonly RedisCircuitBreaker _breaker = breaker;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);

            return;
        }

        if (!Guid.TryParse(context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId))
        {
            await RejectAsync(context);

            return;
        }

        var revokedAt = await TryReadRevokedAtAsync(userId, context.RequestAborted);

        if (revokedAt is not null && IsIssuedBefore(context, revokedAt.Value))
        {
            await RejectAsync(context);

            return;
        }

        await next(context);
    }

    private async Task<DateTimeOffset?> TryReadRevokedAtAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!_breaker.CanTryRedis())
        {
            return null;
        }

        try
        {
            var revokedAt = await _store.GetRevokedAtAsync(userId, cancellationToken);
            _breaker.RecordSuccess();

            return revokedAt;
        }
        catch (Exception exception) when (exception is RedisException or RedisTimeoutException)
        {
            _breaker.RecordFailure(exception);

            return null;
        }
    }

    private static bool IsIssuedBefore(HttpContext context, DateTimeOffset revokedAt)
    {
        var hasIssuedAt = long.TryParse(
            context.User.FindFirst(JwtRegisteredClaimNames.Iat)?.Value,
            out var issuedAtSeconds);

        return !hasIssuedAt || issuedAtSeconds < revokedAt.ToUnixTimeSeconds();
    }

    private static Task RejectAsync(HttpContext context)
    {
        context.Response.Headers[HeaderNames.WWWAuthenticate] = RevocationConstants.InvalidTokenChallenge;

        return context.Response.WriteGatewayProblemAsync(
            StatusCodes.Status401Unauthorized,
            GatewayProblemDetailsConstants.Unauthorized.Title,
            GatewayProblemDetailsConstants.Unauthorized.TypeUrl,
            GatewayProblemDetailsConstants.Unauthorized.Detail,
            context.RequestAborted);
    }
}
