using System.Net;

namespace HealthyFac.Insights.UnitTests.WorkOrders.Data;

/// <summary>
/// Plays Core on the gRPC port. It keeps the request and answers with no gRPC status, so the call ends in an
/// <see cref="Grpc.Core.RpcException"/> after the handlers of the client have seen the request.
/// </summary>
public sealed class CapturingGrpcHandler : HttpMessageHandler
{
    public HttpRequestMessage? Request { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        Request = request;

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Version = HttpVersion.Version20 });
    }
}
