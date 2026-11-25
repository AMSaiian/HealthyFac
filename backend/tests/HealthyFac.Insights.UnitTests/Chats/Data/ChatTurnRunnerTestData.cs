using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Chats.Drafts;
using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Application.Chats.Turns;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.Domain.Chats;
using HealthyFac.Insights.Infrastructure.Agents.Prompts;
using HealthyFac.Insights.Infrastructure.Agents.Tools;
using HealthyFac.Insights.Infrastructure.Ai;
using HealthyFac.Insights.Infrastructure.Chats.Models;
using HealthyFac.Insights.UnitTests.Agents.Data;
using HealthyFac.Insights.UnitTests.Extraction.Data;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Insights.UnitTests.Chats.Data;

public sealed class ScriptedChatTriageModel : IChatTriageModel
{
    public ChatTriageChoice Choice { get; set; } = ChatTriageChoice.InScope;

    public Exception? Failure { get; set; }

    public int CallCount { get; private set; }

    public string ModelName => "triage-model";

    public Task<ChatTriageAnswer> AskAsync(
        string text,
        IReadOnlyList<string> linkLabels,
        CancellationToken cancellationToken
    )
    {
        CallCount++;
        if (Failure is not null)
        {
            throw Failure;
        }

        return Task.FromResult(
            new ChatTriageAnswer
            {
                Verdict = new ChatTriageVerdict { Verdict = Choice },
                InputTokens = 3,
                OutputTokens = 1,
            });
    }
}

public sealed class RecordingChatTurnSaves : IChatTurnSaves
{
    public int SaveCount { get; private set; }

    public AgentRun? Run { get; private set; }

    public bool? WasTokenCancelable { get; private set; }

    public IReadOnlyList<AgentToolCall> ToolCalls { get; private set; } = [];

    public Task SaveAsync(AgentRun run, IReadOnlyList<AgentToolCall> toolCalls, CancellationToken cancellationToken)
    {
        SaveCount++;
        Run = run;
        ToolCalls = toolCalls;
        WasTokenCancelable = cancellationToken.CanBeCanceled;

        return Task.CompletedTask;
    }
}

public sealed class RecordingRealtimeNotifier : IRealtimeNotifier
{
    public List<ChatTurnUpdate> Updates { get; } = [];

    public List<ChatTurnEnd> Ends { get; } = [];

    public int PushCount { get; private set; }

    public void AddChatTurnUpdate(Guid userId, ChatTurnUpdate update) => Updates.Add(update);

    public void AddChatTurnEnd(Guid userId, ChatTurnEnd end) => Ends.Add(end);

    public void Push() => PushCount++;

    public void AddToUser(Guid userId, RealtimeNotificationKind kind, object data, bool isActorSkipped) =>
        throw new NotSupportedException();

    public void AddToGroup(Guid companyId, RealtimeNotificationKind kind, object data, bool isActorSkipped) =>
        throw new NotSupportedException();

    public void AddHint(Guid companyId, RealtimeHint hint) => throw new NotSupportedException();

    public void AddSessionEnd(Guid userId) => throw new NotSupportedException();

    public void Clear()
    {
    }
}

public sealed class TurnRunnerFixture
{
    public TurnRunnerFixture(Action<Chat>? arrange = null, int maxToolCalls = 8, Exception? toolFailure = null)
    {
        State = new ChatTurnState();
        var provider = new ServiceCollection()
            .AddSingleton<IRequestHandler<ChatEmployeesRequest, EchoResponse>>(
                new ChatEmployeesHandler { Failure = toolFailure })
            .AddSingleton<IRequestHandler<ReportNoAnswerToolRequest, ReportNoAnswerResponse>>(
                new ReportNoAnswerToolRequestHandler(State))
            .BuildServiceProvider();
        Time = new FakeTimeProvider(ChatTestData.Now);
        Client = new ScriptedChatClient();
        Triage = new ScriptedChatTriageModel();
        Saves = new RecordingChatTurnSaves();
        Notifier = new RecordingRealtimeNotifier();
        Chat = new Chat(ChatTestData.OwnerId, ChatTestData.CompanyId, ChatTestData.Now);
        arrange?.Invoke(Chat);
        var turn = Chat.AddTurn(ChatTestData.ShortText, ChatTestData.Links, ChatTestData.Now);
        User = turn.User;
        Assistant = turn.Assistant;
        Runner = new ChatTurnRunner(
            Triage,
            new GeminiChatModel(
                Client,
                Options.Create(
                    new GeminiOptions
                    {
                        ProjectId = "project",
                        Location = "location",
                        ChatModel = "chat-model",
                        TriageModel = "triage-model",
                    })),
            new EmbeddedAgentPrompts(),
            Saves,
            Notifier,
            Options.Create(new ChatTurnOptions { MaxToolCalls = maxToolCalls }),
            Time,
            Logger,
            new ToolRegistry(
                new TestAdvisorScopes(),
                [typeof(ChatEmployeesRequest), typeof(ReportNoAnswerToolRequest)],
                Logger,
                new AgentJson(new PrefixIdEncryptor())),
            new ChatToolScopes(provider),
            new FixedChatPermissions(["employees:read"]),
            new FunctionToolCallIds(),
            State,
            new ChatHistoryBuilder(SummaryModel, SummaryLogger),
            new ChatDraftCommit(new ChatDraftBuffer(), new RecordingOutbox()));
    }

    public ChatTurnState State { get; }

    public ScriptedChatSummaryModel SummaryModel { get; } = new();

    public Microsoft.Extensions.Logging.Testing.FakeLogger<ChatHistoryBuilder> SummaryLogger { get; } = new();

    public Microsoft.Extensions.Logging.Testing.FakeLogger<ChatTurnRunner> Logger { get; } = new();

    public FakeTimeProvider Time { get; }

    public ScriptedChatClient Client { get; }

    public ScriptedChatTriageModel Triage { get; }

    public RecordingChatTurnSaves Saves { get; }

    public RecordingRealtimeNotifier Notifier { get; }

    public Chat Chat { get; }

    public HealthyFac.Insights.Domain.Chats.Messages.ChatMessage User { get; }

    public HealthyFac.Insights.Domain.Chats.Messages.ChatMessage Assistant { get; }

    public ChatTurnRunner Runner { get; }

    public Task RunAsync(CancellationToken cancellationToken) =>
        Runner.RunAsync(Chat, User, Assistant, [], cancellationToken);
}

public static class ChatTurnRunnerTestData
{
    public const string FirstPiece = "Check ";
    public const string SecondPiece = "the filter.";
    public const int OverLimitSeconds = 91;

    public static ChatResponseUpdate Piece(string text) => new(ChatRole.Assistant, text);

    public static ChatResponseUpdate ToolCall(string callId, string toolName = ToolTestData.ChatEmployeesToolName) =>
        new(
            ChatRole.Assistant,
            [new FunctionCallContent(callId, toolName, new Dictionary<string, object?> { [key: "text"] = "x" })]);

    public static ChatResponseUpdate Usage(int input, int output) =>
        new()
        {
            Contents = [new UsageContent(new UsageDetails { InputTokenCount = input, OutputTokenCount = output })],
        };

    public static ChatResponseUpdate Length() => new() { FinishReason = ChatFinishReason.Length };

    public static ProviderUnavailableException Unavailable() => new(message: "down", new InvalidOperationException());
}

public sealed class FixedChatPermissions(IReadOnlyCollection<string> permissions) : IChatPermissions
{
    private readonly IReadOnlyCollection<string> _permissions = permissions;

    public IReadOnlyCollection<string> Read() => _permissions;
}

public sealed class ScriptedChatSummaryModel : IChatSummaryModel
{
    public const string Summary = "The user asked about the spindle.";

    public int CallCount { get; private set; }

    public Exception? Failure { get; set; }

    public string? PreviousSummary { get; private set; }

    public IReadOnlyList<ChatPromptMessage> SentMessages { get; private set; } = [];

    public Task<string> SummarizeAsync(
        string? previousSummary,
        IReadOnlyList<ChatPromptMessage> messages,
        CancellationToken cancellationToken
    )
    {
        CallCount++;
        PreviousSummary = previousSummary;
        SentMessages = messages;

        return Failure is null ? Task.FromResult(Summary) : Task.FromException<string>(Failure);
    }
}

public sealed class RecordingOutbox : HealthyFac.Shared.Application.Messaging.IOutbox
{
    public List<object> Messages { get; } = [];

    public void Add<TMessage>(TMessage message)
        where TMessage : HealthyFac.Shared.Application.Messaging.IIntegrationMessage =>
        Messages.Add(message);
}
