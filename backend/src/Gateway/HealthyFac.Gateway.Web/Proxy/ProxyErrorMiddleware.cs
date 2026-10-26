using Yarp.ReverseProxy.Forwarder;

namespace HealthyFac.Gateway.Web.Proxy;

/// <summary>
/// Writes a problem details body when the proxy fails. YARP sets only the status code, so the client would get
/// an empty response. The middleware logs each failure at Warning with the route, the cluster, and the error
/// kind, and never logs a header or a body.
/// </summary>
/// <remarks>
/// A timeout gives <c>504</c>. No available destination gives <c>503</c>. A client cancellation gives no body.
/// Every other forwarder error gives <c>502</c>. A body is written only while the response has not started.
/// The middleware implements <see cref="IMiddleware"/>, so the container creates it. Register it with
/// <c>AddSingleton</c>.
/// </remarks>
internal sealed class ProxyErrorMiddleware(ILogger<ProxyErrorMiddleware> logger) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        await next(context);

        var forwarderError = context.GetForwarderErrorFeature();
        if (forwarderError is null || forwarderError.Error == ForwarderError.RequestCanceled)
        {
            return;
        }

        LogFailure(context, forwarderError.Error);

        if (context.Response.HasStarted)
        {
            return;
        }

        await WriteProblemAsync(context, forwarderError.Error);
    }

    private static Task WriteProblemAsync(HttpContext context, ForwarderError error) =>
        error switch
        {
            ForwarderError.RequestTimedOut => context.Response.WriteGatewayProblemAsync(
                StatusCodes.Status504GatewayTimeout,
                GatewayProblemDetailsConstants.GatewayTimeout.Title,
                GatewayProblemDetailsConstants.GatewayTimeout.TypeUrl,
                GatewayProblemDetailsConstants.GatewayTimeout.Detail,
                context.RequestAborted),
            ForwarderError.NoAvailableDestinations => context.Response.WriteGatewayProblemAsync(
                StatusCodes.Status503ServiceUnavailable,
                GatewayProblemDetailsConstants.ServiceUnavailable.Title,
                GatewayProblemDetailsConstants.ServiceUnavailable.TypeUrl,
                GatewayProblemDetailsConstants.ServiceUnavailable.Detail,
                context.RequestAborted),
            _ => context.Response.WriteGatewayProblemAsync(
                StatusCodes.Status502BadGateway,
                GatewayProblemDetailsConstants.BadGateway.Title,
                GatewayProblemDetailsConstants.BadGateway.TypeUrl,
                GatewayProblemDetailsConstants.BadGateway.Detail,
                context.RequestAborted),
        };

    private void LogFailure(HttpContext context, ForwarderError error)
    {
        var proxyFeature = context.GetReverseProxyFeature();

        logger.LogWarning(
            "Proxy failure on route {RouteId} to cluster {ClusterId}: {ErrorKind}",
            proxyFeature.Route.Config.RouteId,
            proxyFeature.Cluster.Config.ClusterId,
            error);
    }
}
