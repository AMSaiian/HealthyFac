using System.Reflection;
using HealthyFac.Insights.Application;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Application.Chats.Turns;
using HealthyFac.Insights.Application.MetricUnits;
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

    public FakeChatPermissions Permissions { get; } = new();

    public void Register(IServiceCollection services)
    {
        services.AddSingleton<IChatTriageModel>(Triage);
        services.AddSingleton<IChatModel>(Model);
        services.AddSingleton<IChatReads>(Reads);
        services.AddSingleton(Permissions);
        services.AddSingleton<IMetricUnitCatalog>(new FakeMetricUnitCatalog());
        services.AddScoped<IChatPermissions>(provider => provider.GetRequiredService<FakeChatPermissions>());
    }

    public void Reset()
    {
        Triage.Choice = ChatTriageChoice.InScope;
        Model.Reset();
        Reads.Reset();
        Permissions.Granted = null;
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

    /// <summary>Tool calls that the model makes, in order, before it answers (name and arguments as JSON).</summary>
    public List<(string Name, string ArgumentsJson)> ToolCalls { get; } = [];

    /// <summary>A source that the model cancels after its tool calls, like a user. Null means none.</summary>
    public CancellationTokenSource? CancelSource { get; set; }

    /// <summary>An error that the model throws after its tool calls. Null means no error.</summary>
    public Exception? FailureAfterTools { get; set; }

    /// <summary>The history that the last stream received.</summary>
    public IReadOnlyList<ChatPromptMessage> LastHistory { get; private set; } = [];

    public void Reset()
    {
        ToolName = null;
        ToolCalls.Clear();
        FailureAfterTools = null;
        CancelSource = null;
        LastHistory = [];
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
        LastHistory = history;

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

        foreach (var (name, argumentsJson) in ToolCalls)
        {
            var scripted = tools.Functions.Single(candidate => candidate.Name == name);
            var arguments = new Microsoft.Extensions.AI.AIFunctionArguments(
                System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson));

            await scripted.InvokeAsync(arguments, cancellationToken);
        }

        if (CancelSource is not null)
        {
            await CancelSource.CancelAsync();
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (FailureAfterTools is not null)
        {
            throw FailureAfterTools;
        }

        yield return new ChatModelChunk { Text = Answer, InputTokens = 5, OutputTokens = 2 };
    }

    private static TaskCompletionSource NewSource() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>
/// Plays the permissions of the token. Without a script the token holds the permission of every chat tool.
/// </summary>
public sealed class FakeChatPermissions : IChatPermissions
{
    /// <summary>The permissions of the token, or <see langword="null"/> for every permission of a chat tool.</summary>
    public IReadOnlyCollection<string>? Granted { get; set; }

    public IReadOnlyCollection<string> Read() =>
        Granted
        ?? ToolRegistry.FindToolTypes(InsightsApplicationAssembly.Instance)
            .SelectMany(type => type.GetCustomAttribute<AgentToolAttribute>()!.Permissions)
            .ToHashSet();
}

/// <summary>Plays the unit catalog of Telemetry with one unit.</summary>
public sealed class FakeMetricUnitCatalog : IMetricUnitCatalog
{
    public Task<IReadOnlyList<MetricUnit>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MetricUnit>>(
            [new MetricUnit { QuantityKind = "Temperature", Name = "Celsius", Abbreviation = "C" }]);
}
