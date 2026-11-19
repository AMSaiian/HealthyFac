using System.Globalization;
using System.Net;
using Google.GenAI;
using Grpc.Core;

namespace HealthyFac.Insights.Infrastructure.Ingest.ProviderCalls;

/// <summary>
/// Tells transient errors of external services from permanent ones. The ingest retry (<see cref="ProviderResilience"/>)
/// and the agent retry share these rules.
/// </summary>
/// <remarks>
/// A transient error is an <see cref="HttpRequestException"/> without a status code or with the status 408, 429, or
/// 5xx, a timeout of the <c>HttpClient</c> (a <see cref="TaskCanceledException"/> with a <see cref="TimeoutException"/>
/// inside), an <see cref="RpcException"/> with the status <c>Unavailable</c>, <c>DeadlineExceeded</c>, or
/// <c>ResourceExhausted</c>, and a Google.GenAI <see cref="ApiException"/> with one of those codes (it derives from
/// <see cref="HttpRequestException"/>). A permanent error is a configuration or access error that no retry fixes: an
/// HTTP status from 400 to 499 other than 408 and 429 (also from Google.GenAI), and an <see cref="RpcException"/> with
/// another status. A gRPC <c>NotFound</c> is neither: it passes through unchanged, because the caller reads it as
/// "the file is gone".
/// </remarks>
public static class ProviderFailures
{
    private const int TooManyRequests = (int)HttpStatusCode.TooManyRequests;
    private const int ServerErrorFloor = (int)HttpStatusCode.InternalServerError;
    private const int ClientErrorFloor = (int)HttpStatusCode.BadRequest;
    private const int RequestTimeout = (int)HttpStatusCode.RequestTimeout;

    /// <summary>
    /// Tells whether a retry may fix the error.
    /// </summary>
    /// <param name="exception">The error of the call.</param>
    /// <returns><see langword="true"/> for a transient error.</returns>
    public static bool IsTransient(Exception exception) =>
        exception switch
        {
            TaskCanceledException { InnerException: TimeoutException } => true,
            RpcException rpc => IsTransientGrpc(rpc.StatusCode),
            ApiException api => api.StatusCode is RequestTimeout or TooManyRequests or >= ServerErrorFloor,
            HttpRequestException http => http.StatusCode is null
                    or HttpStatusCode.RequestTimeout
                    or HttpStatusCode.TooManyRequests
                || (int)http.StatusCode >= ServerErrorFloor,
            _ => false,
        };

    /// <summary>
    /// Tells whether the error is a configuration or access error that no retry fixes.
    /// </summary>
    /// <param name="exception">The error of the call.</param>
    /// <returns><see langword="true"/> for a permanent error.</returns>
    public static bool IsPermanent(Exception exception) =>
        exception switch
        {
            RpcException rpc => !IsTransientGrpc(rpc.StatusCode) && rpc.StatusCode != StatusCode.NotFound,
            _ => HttpStatusOf(exception) is >= ClientErrorFloor
                and < ServerErrorFloor
                and not RequestTimeout
                and not TooManyRequests,
        };

    /// <summary>
    /// Names the status of an error without the text of the provider.
    /// </summary>
    /// <param name="exception">The error of the call.</param>
    /// <returns>The gRPC status name or the HTTP status number.</returns>
    public static string DescribeStatus(Exception exception) =>
        exception is RpcException rpc
            ? rpc.StatusCode.ToString()
            : (HttpStatusOf(exception) ?? 0).ToString(CultureInfo.InvariantCulture);

    private static int? HttpStatusOf(Exception exception) =>
        exception switch
        {
            ApiException api => api.StatusCode,
            HttpRequestException http => (int?)http.StatusCode,
            _ => null,
        };

    private static bool IsTransientGrpc(StatusCode code) =>
        code is StatusCode.Unavailable or StatusCode.DeadlineExceeded or StatusCode.ResourceExhausted;
}
