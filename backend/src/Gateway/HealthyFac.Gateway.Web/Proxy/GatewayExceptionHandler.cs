using Microsoft.AspNetCore.Diagnostics;

namespace HealthyFac.Gateway.Web.Proxy;

/// <summary>
/// Turns an exception of the gateway itself into a <c>500</c> problem details body. The body never carries
/// exception details. A client that closed the connection gets no body.
/// </summary>
internal sealed class GatewayExceptionHandler(ILogger<GatewayExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;

            return true;
        }

        logger.LogError(exception, "Unhandled exception in the gateway");

        await httpContext.Response.WriteGatewayProblemAsync(
            StatusCodes.Status500InternalServerError,
            GatewayProblemDetailsConstants.InternalServerError.Title,
            GatewayProblemDetailsConstants.InternalServerError.TypeUrl,
            GatewayProblemDetailsConstants.InternalServerError.Detail,
            cancellationToken);

        return true;
    }
}
