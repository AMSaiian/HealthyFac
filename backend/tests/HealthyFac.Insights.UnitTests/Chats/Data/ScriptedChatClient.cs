using Microsoft.Extensions.AI;

namespace HealthyFac.Insights.UnitTests.Chats.Data;

/// <summary>
/// A chat client that answers from a script. A call without a streaming script answers with the queued response, and a
/// streaming call plays the queued list of updates. Each update waits for <see cref="BeforeUpdate"/>, so a test can
/// move a fake clock or cancel between the pieces of one answer.
/// </summary>
public sealed class ScriptedChatClient : IChatClient
{
    private readonly Queue<ChatResponse> _responses = new();
    private readonly Queue<IReadOnlyList<ChatResponseUpdate>> _streams = new();

    public int CallCount { get; private set; }

    public Exception? Failure { get; set; }

    /// <summary>Runs before each streamed update with the zero-based index of the update.</summary>
    public Func<int, Task>? BeforeUpdate { get; set; }

    /// <summary>The options of each call.</summary>
    public List<ChatOptions?> SentOptions { get; } = [];

    /// <summary>The text of each call: the system prompt first, then the text of every message.</summary>
    public List<string> SentTexts { get; } = [];

    /// <summary>The user messages of each call, one list for each call.</summary>
    public List<IReadOnlyList<ChatMessage>> SentMessages { get; } = [];

    public void ScriptText(string text, UsageDetails? usage = null) =>
        _responses.Enqueue(new ChatResponse(new ChatMessage(ChatRole.Assistant, text)) { Usage = usage });

    public void ScriptStream(params ChatResponseUpdate[] updates) => _streams.Enqueue(updates);

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        Record(messages, options);
        await Task.CompletedTask;

        if (Failure is not null)
        {
            throw Failure;
        }

        return _responses.Dequeue();
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken =
            default(CancellationToken)
    )
    {
        Record(messages, options);

        if (Failure is not null)
        {
            throw Failure;
        }

        var updates = _streams.Dequeue();

        for (var index = 0; index < updates.Count; index++)
        {
            if (BeforeUpdate is not null)
            {
                await BeforeUpdate(index);
            }

            cancellationToken.ThrowIfCancellationRequested();

            yield return updates[index];
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    private void Record(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        CallCount++;
        var sent = messages.ToList();
        SentOptions.Add(options);
        SentMessages.Add(sent);
        SentTexts.Add(string.Join(Environment.NewLine, [options?.Instructions, .. sent.Select(m => m.Text)]));
    }
}
