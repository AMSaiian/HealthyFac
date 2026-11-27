using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace HealthyFac.Insights.ApiTests.Chats.Data;

/// <summary>A chat client that streams one text update and counts its streaming calls.</summary>
public sealed class RecordingAssistantChatClient : IChatClient
{
    public const string AnswerText = "The assistant client answered.";

    public int StreamingCallCount { get; private set; }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        throw new NotSupportedException();

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        StreamingCallCount++;
        await Task.Yield();

        yield return new ChatResponseUpdate(ChatRole.Assistant, AnswerText);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
