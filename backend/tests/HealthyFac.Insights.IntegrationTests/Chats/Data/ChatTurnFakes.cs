using System.Reflection;
using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Application.Chats.Turns;
using HealthyFac.Insights.IntegrationTests.Agents.Data;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.IntegrationTests.Chats.Data;

/// <summary>
/// Replaces the triage model and the chat model with doubles, so a test runs a turn without a provider.
/// </summary>
public sealed class ChatTurnFakes
{
    public ScriptedChatTriage Triage { get; } = new();

    public ScriptedChatModel Model { get; } = new();

    public FakeChatReads Reads { get; } = new();

    public void Register(IServiceCollection services)
    {
        services.AddSingleton<IChatTriageModel>(Triage);
        services.AddSingleton<IChatModel>(Model);
        services.AddSingleton<IChatReads>(Reads);
        services.AddScoped<IChatPermissions>(_ => new AllChatPermissions());
    }

    public void Reset()
    {
        Triage.Choice = ChatTriageChoice.InScope;
        Model.Reset();
        Reads.Reset();
        Reads.MachineAnswer = ChatTestData.MachineAnswer();
    }
}

public sealed class ScriptedChatTriage : IChatTriageModel
{
    public ChatTriageChoice Choice { get; set; } = ChatTriageChoice.InScope;

    public string ModelName => "triage-double";

    public Task<ChatTriageAnswer> AskAsync(
        string text,
        IReadOnlyList<string> linkLabels,
        CancellationToken cancellationToken
    ) =>
        Task.FromResult(
            new ChatTriageAnswer
            {
                Verdict = new ChatTriageVerdict { Verdict = Choice },
                InputTokens = 1,
                OutputTokens = 1,
            });
}

public sealed class ScriptedChatModel : IChatModel
{
    private const int StartTimeoutSeconds = 10;

    private readonly object _lock = new();

    private TaskCompletionSource _allStarted = NewSource();
    private int _startedCount;

    public string ModelName => "chat-double";

    public string Answer { get; set; } = ChatTestData.Answer;

    /// <summary>The number of streams that must have started before any stream answers. 0 means no wait.</summary>
    public int ConcurrentStreamCount { get; set; }

    public int CallCount { get; private set; }

    /// <summary>The name of a tool that the model calls once before it answers. Null means no tool call.</summary>
    public string? ToolName { get; set; }

    public void Reset()
    {
        ToolName = null;
        Answer = ChatTestData.Answer;
        ConcurrentStreamCount = 0;
        CallCount = 0;
        _startedCount = 0;
        _allStarted = NewSource();
    }

    public async IAsyncEnumerable<ChatModelChunk> StreamAsync(
        string systemPrompt,
        IReadOnlyList<ChatPromptMessage> history,
        ChatTurnTools tools,
        [System.Runtime.CompilerServices.EnumeratorCancellation]
        CancellationToken cancellationToken
    )
    {
        lock (_lock)
        {
            CallCount++;
            _startedCount++;
            if (_startedCount >= ConcurrentStreamCount)
            {
                _allStarted.TrySetResult();
            }
        }

        await _allStarted.Task.WaitAsync(TimeSpan.FromSeconds(StartTimeoutSeconds), cancellationToken);

        if (ToolName is not null)
        {
            var function = tools.Functions.Single(candidate => candidate.Name == ToolName);

            await function.InvokeAsync(new Microsoft.Extensions.AI.AIFunctionArguments(), cancellationToken);
        }

        yield return new ChatModelChunk { Text = Answer, InputTokens = 5, OutputTokens = 2 };
    }

    private static TaskCompletionSource NewSource() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>Plays a token that holds the permission of every chat tool.</summary>
public sealed class AllChatPermissions : IChatPermissions
{
    public IReadOnlyCollection<string> Read() =>
        ToolRegistry.FindToolTypes(InsightsApplicationAssembly.Instance)
            .SelectMany(type => type.GetCustomAttribute<AgentToolAttribute>()!.Permissions)
            .ToHashSet();
}
