using Microsoft.Extensions.AI;

namespace HealthyFac.Insights.UnitTests.Extraction.Data;

/// <summary>
/// A chat client that answers from a script of texts and records each call.
/// </summary>
public sealed class ScriptedChatClient : IChatClient
{
    private readonly Queue<string> _answers = new();

    public List<RecordedChatCall> Calls { get; } = [];

    public void Script(params string[] answers)
    {
        foreach (var answer in answers)
        {
            _answers.Enqueue(answer);
        }
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        Calls.Add(new RecordedChatCall([.. messages], options));
        var answer = _answers.Count > 0 ? _answers.Dequeue() : ExtractionTestData.EmptyAnswer;

        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, answer)));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default(CancellationToken)
    ) => throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}

/// <summary>
/// One recorded call to the chat client.
/// </summary>
/// <param name="Messages">The messages of the call, with the system prompt first.</param>
/// <param name="Options">The chat options of the call.</param>
public sealed record RecordedChatCall(IReadOnlyList<ChatMessage> Messages, ChatOptions? Options);
