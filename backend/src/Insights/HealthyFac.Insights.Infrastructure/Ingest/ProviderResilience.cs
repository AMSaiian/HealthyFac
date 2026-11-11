using System.Globalization;
using System.Net;
using Google.GenAI;
using Grpc.Core;
using HealthyFac.Insights.Application.Ingest.Processing;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace HealthyFac.Insights.Infrastructure.Ingest;

/// <summary>
/// Runs the calls to external services (Document, blob storage, Docling, Telemetry, Gemini) in one retry pipeline: 3
/// retries with a delay that doubles, only for transient errors. After the last retry, the transient error becomes a
/// <see cref="ProviderUnavailableException"/>. A permanent error becomes the same exception at once, with no retry.
/// Another error passes through unchanged.
/// </summary>
/// <remarks>
/// A transient error is an <see cref="HttpRequestException"/> without a status code or with the status 408, 429, or
/// 5xx, a timeout of the <c>HttpClient</c> (a <see cref="TaskCanceledException"/> with a <see cref="TimeoutException"/>
/// inside), an <see cref="RpcException"/> with the status <c>Unavailable</c>, <c>DeadlineExceeded</c>, or
/// <c>ResourceExhausted</c>, and a Google.GenAI <see cref="ApiException"/> with one of those codes (it derives from
/// <see cref="HttpRequestException"/>). A permanent error is a configuration or access error that no retry fixes: an
/// HTTP status from 400 to 499 other than 408 and 429 (also from Google.GenAI), and an <see cref="RpcException"/> with
/// another status. The attempt must end at once, not wait for the lease sweep. A gRPC <c>NotFound</c> passes through
/// unchanged, because the pipeline reads it as "the file is gone". The message of a permanent error holds the status
/// only, never the text of the provider.
/// </remarks>
public sealed class ProviderResilience : IProviderCalls
{
    public const int MaxRetries = 3;

    private const int TooManyRequests = (int)HttpStatusCode.TooManyRequests;
    private const int ServerErrorFloor = (int)HttpStatusCode.InternalServerError;
    private const int ClientErrorFloor = (int)HttpStatusCode.BadRequest;
    private const int RequestTimeout = (int)HttpStatusCode.RequestTimeout;
    private const string UnavailableMessage = "An external service stayed unavailable after {0} retries.";
    private const string RejectedMessage = "An external service rejected the call with the status {0}.";

    private readonly ResiliencePipeline _pipeline;

    public ProviderResilience(IOptions<IngestProcessingOptions> options, TimeProvider timeProvider)
    {
        _pipeline = new ResiliencePipelineBuilder { TimeProvider = timeProvider }
            .AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = MaxRetries,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromMilliseconds(options.Value.RetryDelayMilliseconds),
                    UseJitter = false,
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(IsTransient),
                })
            .Build();
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> call,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await _pipeline.ExecuteAsync(async token => await call(token), cancellationToken);
        }
        catch (Exception exception) when (IsTransient(exception) && !cancellationToken.IsCancellationRequested)
        {
            throw new ProviderUnavailableException(string.Format(UnavailableMessage, MaxRetries), exception);
        }
        catch (Exception exception) when (IsPermanent(exception) && !cancellationToken.IsCancellationRequested)
        {
            throw new ProviderUnavailableException(
                string.Format(RejectedMessage, DescribeStatus(exception)),
                exception);
        }
    }

    private static int? HttpStatusOf(Exception exception) =>
        exception switch
        {
            ApiException api => api.StatusCode,
            HttpRequestException http => (int?)http.StatusCode,
            _ => null,
        };

    private static string DescribeStatus(Exception exception) =>
        exception is RpcException rpc
            ? rpc.StatusCode.ToString()
            : (HttpStatusOf(exception) ?? 0).ToString(CultureInfo.InvariantCulture);

    private static bool IsPermanent(Exception exception) =>
        exception switch
        {
            RpcException rpc => !IsTransientGrpc(rpc.StatusCode) && rpc.StatusCode != StatusCode.NotFound,
            _ => HttpStatusOf(exception) is >= ClientErrorFloor and < ServerErrorFloor
                and not RequestTimeout
                and not TooManyRequests,
        };

    private static bool IsTransientGrpc(StatusCode code) =>
        code is StatusCode.Unavailable or StatusCode.DeadlineExceeded or StatusCode.ResourceExhausted;

    private static bool IsTransient(Exception exception) =>
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
}
