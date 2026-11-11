using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Web.Rpc;

/// <summary>
/// Keeps gRPC calls on the gRPC port and every other request off it. A mismatch gets <c>404</c>, so the public API
/// shows no gRPC route and the gRPC port shows no HTTP route.
/// </summary>
/// <remarks>
/// It compares the local port of the connection, which a client cannot change. It never reads the <c>Host</c>
/// header. It tells a gRPC call by its content type, so it runs before routing.
/// </remarks>
public sealed class GrpcPortMiddleware(IOptions<GrpcEndpointOptions> options) : IMiddleware
{
    private readonly int _grpcPort = options.Value.Port;

    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var isGrpcPort = context.Connection.LocalPort == _grpcPort;
        var isGrpcCall = context.Request.ContentType?.StartsWith(
                GrpcConstants.ContentType,
                StringComparison.OrdinalIgnoreCase)
            == true;

        if (isGrpcPort != isGrpcCall)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;

            return Task.CompletedTask;
        }

        return next(context);
    }
}
