using System.Net;
using System.Text.Json;
using Google.GenAI;
using Grpc.Core;

namespace HealthyFac.Insights.UnitTests.Ingest.Data;

public static class ProviderResilienceTestData
{
    /// <summary>The text that a provider puts into its error. No message of ours may hold it.</summary>
    public const string ProviderText = "secret provider detail";

    public static TheoryData<Exception> PermanentErrors => new()
    {
        { Http(HttpStatusCode.Unauthorized) },
        { Http(HttpStatusCode.Forbidden) },
        { Http(HttpStatusCode.NotFound) },
        { Http(HttpStatusCode.BadRequest) },
        { new ClientError(ProviderText, 404, "NOT_FOUND", null) },
        { new RpcException(new Status(StatusCode.PermissionDenied, ProviderText)) },
        { new RpcException(new Status(StatusCode.Unauthenticated, ProviderText)) },
        { new RpcException(new Status(StatusCode.Internal, ProviderText)) },
    };

    public static TheoryData<Exception> TransientErrors => new()
    {
        { Http(HttpStatusCode.RequestTimeout) },
        { Http(HttpStatusCode.TooManyRequests) },
        { Http(HttpStatusCode.ServiceUnavailable) },
        { new HttpRequestException(ProviderText) },
        { new RpcException(new Status(StatusCode.Unavailable, ProviderText)) },
        { new RpcException(new Status(StatusCode.DeadlineExceeded, ProviderText)) },
        { new TaskCanceledException("Timeout", new TimeoutException()) },
    };

    public static TheoryData<Exception> PassThroughErrors => new()
    {
        { new RpcException(new Status(StatusCode.NotFound, ProviderText)) },
        { new JsonException(ProviderText) },
        { new InvalidOperationException(ProviderText) },
    };

    private static HttpRequestException Http(HttpStatusCode status) => new(ProviderText, inner: null, status);
}
