using System.Diagnostics;
using HealthyFac.Shared.Application.BackgroundQueues;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Infrastructure.Caching;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using StackExchange.Redis;
using Results = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Realtime.Results;
using Tags = HealthyFac.Shared.Application.Observability.ObservabilityConstants.Realtime.Tags;

namespace HealthyFac.Shared.Web.Realtime;

/// <summary>
/// Sends one push through the hub context. The hub context writes to the Redis backplane, so each attempt runs
/// through the shared Redis pipeline (the breaker) inside the retry pipeline of the push.
/// </summary>
/// <remarks>
/// A <see cref="BrokenCircuitException"/> is never retried: the push is dropped at once. A failure after the last
/// retry is logged at <c>Warning</c> without any user ID and counted as failed. A push that has no listener is lost,
/// because a push is a hint and the web app loads the data again after a reconnect.
/// </remarks>
/// <param name="hubContext">The hub context of the host.</param>
/// <param name="redis">The shared Redis pipeline with the circuit breaker.</param>
/// <param name="retry">The retry pipeline of the push.</param>
/// <param name="metrics">Counts the pushes and records their duration.</param>
/// <param name="timeProvider">The clock for the duration of a push.</param>
/// <param name="logger">The logger of the handler.</param>
public sealed class RealtimePushHandler(
    IHubContext<RealtimeHub, IRealtimeClient> hubContext,
    RedisResiliencePipeline redis,
    [FromKeyedServices(RealtimeConstants.Push.PipelineKey)]
    ResiliencePipeline retry,
    RealtimeMetrics metrics,
    TimeProvider timeProvider,
    ILogger<RealtimePushHandler> logger
) : IBackgroundItemHandler<RealtimePush>
{
    private readonly IHubContext<RealtimeHub, IRealtimeClient> _hubContext = hubContext;
    private readonly RedisResiliencePipeline _redis = redis;
    private readonly ResiliencePipeline _retry = retry;
    private readonly RealtimeMetrics _metrics = metrics;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<RealtimePushHandler> _logger = logger;

    public async Task HandleAsync(RealtimePush item, ActivityContext parentContext, CancellationToken cancellationToken)
    {
        using var activity = RealtimeActivitySource.Instance.StartActivity(
            ObservabilityConstants.Spans.RealtimePush,
            ActivityKind.Internal,
            parentContext);
        activity.WithTag(Tags.Target, item.MetricTarget);

        var startedAt = _timeProvider.GetTimestamp();
        var result = await SendWithResultAsync(item, cancellationToken);

        activity.WithTag(Tags.Result, result);
        _metrics.RecordPush(item.MetricTarget, result, _timeProvider.GetElapsedTime(startedAt));
    }

    private async Task<string> SendWithResultAsync(RealtimePush item, CancellationToken cancellationToken)
    {
        try
        {
            await _retry.ExecuteAsync(
                async token => await _redis.ExecuteAsync(innerToken => SendAsync(item, innerToken), token),
                cancellationToken);

            return Results.Sent;
        }
        catch (BrokenCircuitException)
        {
            return Results.Dropped;
        }
        catch (Exception exception) when (
            exception is RedisException or RedisTimeoutException
            && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                exception,
                message: "A real-time push of target {Target} failed after the last retry.",
                item.MetricTarget);

            return Results.Failed;
        }
    }

    private async ValueTask SendAsync(RealtimePush item, CancellationToken cancellationToken)
    {
        var client = item.Target == RealtimePushTarget.User
            ? _hubContext.Clients.User(item.UserId!.Value.ToString(format: "D"))
            : _hubContext.Clients.Group(item.Group!);

        if (item.IsSessionEnd)
        {
            await client.SessionEnded().WaitAsync(cancellationToken);

            return;
        }

        if (item.Notification is not null)
        {
            await client.Notification(item.Notification).WaitAsync(cancellationToken);

            return;
        }

        await client.Hint(item.Hint!).WaitAsync(cancellationToken);
    }
}
