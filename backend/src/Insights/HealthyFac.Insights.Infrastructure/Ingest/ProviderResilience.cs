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
/// <see cref="ProviderFailures"/> tells the kinds of error apart. The attempt must end at once on a permanent error,
/// not wait for the lease sweep. The message of a permanent error holds the status only, never the text of the
/// provider.
/// </remarks>
public sealed class ProviderResilience : IProviderCalls
{
    public const int MaxRetries = 3;

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
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(ProviderFailures.IsTransient),
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
        catch (Exception exception) when (ProviderFailures.IsTransient(exception)
            && !cancellationToken.IsCancellationRequested)
        {
            throw new ProviderUnavailableException(string.Format(UnavailableMessage, MaxRetries), exception);
        }
        catch (Exception exception) when (ProviderFailures.IsPermanent(exception)
            && !cancellationToken.IsCancellationRequested)
        {
            throw new ProviderUnavailableException(
                string.Format(RejectedMessage, ProviderFailures.DescribeStatus(exception)),
                exception);
        }
    }
}
