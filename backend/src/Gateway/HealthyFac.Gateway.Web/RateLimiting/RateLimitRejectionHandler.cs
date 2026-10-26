using System.Globalization;
using System.Threading.RateLimiting;
using HealthyFac.Gateway.Web.Proxy;
using Microsoft.AspNetCore.RateLimiting;
using RedisRateLimiting;

namespace HealthyFac.Gateway.Web.RateLimiting;

/// <summary>
/// Answers a rate limited request with <c>429</c>, a <c>Retry-After</c> header, and a problem details body.
/// </summary>
/// <remarks>
/// A rejection is logged at Debug, so an attack does not flood the logs. The Redis limiter reports the retry
/// time as whole seconds under its own metadata name. The local limiter reports a <see cref="TimeSpan"/> under the
/// framework metadata name. The handler reads both.
/// </remarks>
internal sealed class RateLimitRejectionHandler(ILogger<RateLimitRejectionHandler> logger)
{
    public async ValueTask HandleAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        logger.LogDebug("Rate limit rejected a request to {Path}", context.HttpContext.Request.Path);

        if (TryGetRetryAfterSeconds(context, out var retryAfterSeconds))
        {
            context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        }

        await context.HttpContext.Response.WriteGatewayProblemAsync(
            StatusCodes.Status429TooManyRequests,
            GatewayProblemDetailsConstants.TooManyRequests.Title,
            GatewayProblemDetailsConstants.TooManyRequests.TypeUrl,
            GatewayProblemDetailsConstants.TooManyRequests.Detail,
            cancellationToken);
    }

    private static bool TryGetRetryAfterSeconds(OnRejectedContext context, out int retryAfterSeconds)
    {
        if (context.Lease.TryGetMetadata(RateLimitMetadataName.RetryAfter, out retryAfterSeconds))
        {
            return true;
        }

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            retryAfterSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds);

            return true;
        }

        retryAfterSeconds = 0;

        return false;
    }
}
