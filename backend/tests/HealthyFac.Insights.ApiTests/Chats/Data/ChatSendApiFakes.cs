using HealthyFac.Insights.Application.Chats.Turns;
using HealthyFac.Shared.Application.BackgroundQueues;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Testing.Realtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HealthyFac.Insights.ApiTests.Chats.Data;

/// <summary>
/// Replaces the models of a chat turn and the realtime queue of the host, so a test runs a turn without a provider and
/// reads the pushes.
/// </summary>
public sealed class ChatSendApiFakes
{
    public const string PartialText = "Check ";

    public BlockingChatModel Model { get; } = new();

    public RecordingRealtimeQueue Queue { get; } = new();

    public void Register(IServiceCollection services)
    {
        services.RemoveAll<IChatTriageModel>();
        services.RemoveAll<IChatModel>();
        services.RemoveAll<IBackgroundQueue<RealtimePush>>();
        services.AddSingleton<IChatTriageModel>(new InScopeTriage());
        services.AddSingleton<IChatModel>(Model);
        services.AddSingleton<IBackgroundQueue<RealtimePush>>(Queue);
    }

    private sealed class InScopeTriage : IChatTriageModel
    {
        public string ModelName => "triage-double";

        public Task<ChatTriageAnswer> AskAsync(
            string text,
            IReadOnlyList<string> linkLabels,
            CancellationToken cancellationToken
        ) =>
            Task.FromResult(
                new ChatTriageAnswer
                {
                    Verdict = new ChatTriageVerdict { Verdict = ChatTriageChoice.InScope },
                    InputTokens = 1,
                    OutputTokens = 1,
                });
    }
}

/// <summary>
/// A chat model that answers at once, or, when <see cref="IsBlocking"/> is set, sends a first piece and then waits
/// until the token is cancelled.
/// </summary>
public sealed class BlockingChatModel : IChatModel
{
    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public string ModelName => "chat-double";

    public bool IsBlocking { get; set; }

    public Task Started => _started.Task;

    public async IAsyncEnumerable<ChatModelChunk> StreamAsync(
        string systemPrompt,
        IReadOnlyList<ChatPromptMessage> history,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken
    )
    {
        yield return new ChatModelChunk { Text = IsBlocking ? ChatSendApiFakes.PartialText : ChatApiTestData.Answer };
        if (IsBlocking)
        {
            _started.TrySetResult();
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }
}
