using Microsoft.Extensions.AI;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Insights.UnitTests.Chats.Data;

/// <summary>
/// A chat client whose streams are scripts. Each request plays the next script: an update, a wait on the fake clock,
/// or a stall that never ends until the caller cancels the request.
/// </summary>
/// <param name="time">The fake clock that the waits use.</param>
public sealed class StallScriptChatClient(FakeTimeProvider time) : IChatClient
{
    private readonly Queue<IReadOnlyList<StallStep>> _scripts = new();
    private readonly FakeTimeProvider _time = time;

    public int RequestCount { get; private set; }

    public void Script(params StallStep[] steps) => _scripts.Enqueue(steps);

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken =
            default(CancellationToken)
    )
    {
        RequestCount++;

        foreach (var step in _scripts.Dequeue())
        {
            if (step.Wait is { } wait)
            {
                await Task.Delay(wait, _time, cancellationToken);
            }

            if (step.IsStall)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            if (step.Text is { } text)
            {
                yield return new ChatResponseUpdate(ChatRole.Assistant, text);
            }
        }
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}

/// <summary>One step of a script: an update, a wait before it, or a stall.</summary>
public sealed record StallStep(string? Text = null, TimeSpan? Wait = null, bool IsStall = false);
