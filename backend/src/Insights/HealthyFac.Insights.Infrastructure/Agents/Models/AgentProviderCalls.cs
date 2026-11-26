using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Infrastructure.Ingest.ProviderCalls;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace HealthyFac.Insights.Infrastructure.Agents.Models;

/// <summary>
/// Runs the calls of an agent run in one retry pipeline: one retry for each entry of
/// <see cref="AgentOptions.ProviderRetryDelaysSeconds"/>, each after the delay of its index, only for transient
/// errors (<see cref="ProviderFailures"/>). After the last retry, the transient error becomes a
/// <see cref="ProviderUnavailableException"/>. A permanent error becomes the same exception at once, with no retry.
/// Another error passes through unchanged. The message of the exception holds the status only, never the text of the
/// provider.
/// </summary>
public sealed class AgentProviderCalls : IAgentProviderCalls
{
    private const string UnavailableMessage = "The model stayed unavailable after {0} retries.";
    private const string RejectedMessage = "The model rejected the call with the status {0}.";

    private readonly ResiliencePipeline _pipeline;
    private readonly int _retryCount;

    public AgentProviderCalls(IOptions<AgentOptions> options, TimeProvider timeProvider)
    {
        var delays = options.Value.ProviderRetryDelaysSeconds;
        _retryCount = delays.Count;

        _pipeline = new ResiliencePipelineBuilder { TimeProvider = timeProvider }
            .AddRetry(
                new RetryStrategyOptions
                {
                    MaxRetryAttempts = delays.Count,
                    DelayGenerator = arguments =>
                        new ValueTask<TimeSpan?>(TimeSpan.FromSeconds(delays[arguments.AttemptNumber])),
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
            throw new ProviderUnavailableException(string.Format(UnavailableMessage, _retryCount), exception);
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
