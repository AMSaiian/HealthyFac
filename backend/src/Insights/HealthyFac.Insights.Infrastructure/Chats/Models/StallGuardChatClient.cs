using System.Runtime.CompilerServices;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Insights.Infrastructure.Chats.Models;

/// <summary>
/// Ends a model stream that sends no data for the idle timeout. The provider can return the response headers and then
/// send no body, and the resilience timeout of the HTTP client covers only the headers, so such a request would hold
/// the whole turn until its time limit. A stall before the first update sends the same request once more, because the
/// user saw no text yet. A second stall, or a stall after an update, throws a
/// <see cref="ProviderUnavailableException"/>. The guard wraps the raw client, so each request of a tool loop is
/// guarded on its own and a retry never runs a tool again.
/// </summary>
/// <param name="inner">The chat client that calls the provider.</param>
/// <param name="idleTimeout">The longest wait for the next update.</param>
/// <param name="timeProvider">The clock of the idle timer.</param>
/// <param name="logger">Logs each stall that the guard retries.</param>
public sealed class StallGuardChatClient(
    IChatClient inner,
    TimeSpan idleTimeout,
    TimeProvider timeProvider,
    ILogger logger
) : DelegatingChatClient(inner)
{
    public const string StalledMessage = "The chat model sent no data within the idle timeout.";

    private const int MaxAttempts = 2;

    private readonly TimeSpan _idleTimeout = idleTimeout;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger _logger = logger;

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var request = messages.ToList();

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            using var idle = new CancellationTokenSource(Timeout.InfiniteTimeSpan, _timeProvider);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, idle.Token);

            await using var enumerator = base.GetStreamingResponseAsync(request, options, linked.Token)
                .GetAsyncEnumerator(linked.Token);

            var hasUpdate = false;
            var isStalled = false;
            var startedAt = _timeProvider.GetTimestamp();

            while (!isStalled)
            {
                bool hasNext;
                idle.CancelAfter(_idleTimeout);

                try
                {
                    hasNext = await enumerator.MoveNextAsync();
                }
                catch (OperationCanceledException) when (
                    idle.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    isStalled = true;

                    continue;
                }

                idle.CancelAfter(Timeout.InfiniteTimeSpan);

                if (!hasNext)
                {
                    yield break;
                }

                if (!hasUpdate)
                {
                    _logger.LogDebug(
                        message:
                        "A chat model request sent its first update after {Milliseconds} ms (attempt {Attempt}).",
                        _timeProvider.GetElapsedTime(startedAt).TotalMilliseconds,
                        attempt);
                }

                hasUpdate = true;

                yield return enumerator.Current;
            }

            var isLastAttempt = attempt == MaxAttempts;

            if (hasUpdate || isLastAttempt)
            {
                throw new ProviderUnavailableException(StalledMessage);
            }

            _logger.LogWarning(message: "A chat model request stalled before its first update. It is sent again.");
        }
    }
}
