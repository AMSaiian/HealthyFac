using HealthyFac.Gateway.Web.Proxy;
using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Shared.Infrastructure.Revocation;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.Net.Http.Headers;
using Polly.CircuitBreaker;
using StackExchange.Redis;

namespace HealthyFac.Gateway.Web.Revocation;

/// <summary>
/// Rejects a request when its token was issued before the revocation time of the user. Redis is not a hard
/// dependency: when the Redis circuit breaker is open or Redis fails, the request passes, and the breaker logs the
/// state change. Run the middleware after authentication.
/// </summary>
/// <param name="store">The revocation list.</param>
/// <param name="redis">The shared Redis pipeline that protects the gateway from a slow Redis.</param>
public sealed class RevocationCheckMiddleware(
    ITokenRevocationStore store,
    RedisResiliencePipeline redis
) : IMiddleware
{
    private readonly ITokenRevocationStore _store = store;
    private readonly RedisResiliencePipeline _redis = redis;

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
        try
        {
            var revokedAt = await _redis.ExecuteAsync(
                token => new ValueTask<DateTimeOffset?>(_store.GetRevokedAtAsync(userId, token)),
                cancellationToken);

            return revokedAt;
        }
        catch (Exception exception)
            when (exception is RedisException or RedisTimeoutException or BrokenCircuitException)
        {
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
