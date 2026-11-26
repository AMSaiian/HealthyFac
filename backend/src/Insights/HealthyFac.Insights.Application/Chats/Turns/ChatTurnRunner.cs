using HealthyFac.Insights.Application.Agents.Prompts;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Chats.Drafts;
using HealthyFac.Insights.Application.Chats.Links;
using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Chats;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Shared.Application.Realtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Application.Chats.Turns;

/// <summary>
/// Runs one chat turn: the triage, then the streamed answer of the chat model. The turn ends in one final save and
/// one <see cref="ChatTurnEnd"/> push, whatever the ending is.
/// </summary>
/// <remarks>
/// The time limit is a source that is linked to the token of the request and runs on the injected clock. The final
/// save uses <see cref="CancellationToken.None"/> for every ending (form <b>Finish</b>): the turn already ran, and a
/// cancelled token must never leave the reply <c>streaming</c> or lose the partial text. The save is one unit and
/// has no later durable step except the push. An <see cref="OperationCanceledException"/> of the request or of the
/// limit is an ending, never a failure record.
/// </remarks>
/// <param name="triage">Gives the verdict on the new message.</param>
/// <param name="model">Streams the answer.</param>
/// <param name="prompts">Provides the system prompt and its version.</param>
/// <param name="saves">Stores the final state.</param>
/// <param name="notifier">Pushes the snapshots and the end.</param>
/// <param name="options">Holds the limits.</param>
/// <param name="timeProvider">The clock of the limit and of the push interval.</param>
/// <param name="logger">Logs an unexpected error of a turn once, without the text of the user.</param>
/// <param name="registry">Builds the tools of the turn.</param>
/// <param name="toolScopes">Runs each tool in the request scope of the user.</param>
/// <param name="permissions">Reads the permissions of the token.</param>
/// <param name="toolCallIds">Gives the call ID and ends the function loop.</param>
/// <param name="state">Tells whether the model reported no answer.</param>
/// <param name="historyBuilder">Builds the model input and compacts a long chat.</param>
/// <param name="draftCommit">Stores the draft rows and sends the proposals of a completed turn.</param>
public sealed class ChatTurnRunner(
    IChatTriageModel triage,
    IChatModel model,
    IAgentPrompts prompts,
    IChatTurnSaves saves,
    IRealtimeNotifier notifier,
    IOptions<ChatTurnOptions> options,
    TimeProvider timeProvider,
    ILogger<ChatTurnRunner> logger,
    ToolRegistry registry,
    ChatToolScopes toolScopes,
    IChatPermissions permissions,
    IToolCallIds toolCallIds,
    ChatTurnState state,
    ChatHistoryBuilder historyBuilder,
    ChatDraftCommit draftCommit
)
{
    private readonly IChatTriageModel _triage = triage;
    private readonly IChatModel _model = model;
    private readonly IAgentPrompts _prompts = prompts;
    private readonly IChatTurnSaves _saves = saves;
    private readonly IRealtimeNotifier _notifier = notifier;
    private readonly ChatTurnOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<ChatTurnRunner> _logger = logger;
    private readonly ToolRegistry _registry = registry;
    private readonly ChatToolScopes _toolScopes = toolScopes;
    private readonly IChatPermissions _permissions = permissions;
    private readonly IToolCallIds _toolCallIds = toolCallIds;
    private readonly ChatTurnState _state = state;
    private readonly ChatHistoryBuilder _historyBuilder = historyBuilder;
    private readonly ChatDraftCommit _draftCommit = draftCommit;

    /// <summary>
    /// Runs the turn of the chat. The chat and its messages are tracked by the database context, so the final save
    /// stores them with the run row.
    /// </summary>
    /// <param name="chat">The chat of the turn, with its messages.</param>
    /// <param name="userMessage">The new message of the user.</param>
    /// <param name="assistantMessage">The reply that streams.</param>
    /// <param name="links">The links of the new message, resolved as the user.</param>
    /// <param name="cancellationToken">The token of the request. A cancel ends the turn <c>cancelled</c>.</param>
    public async Task RunAsync(
        Chat chat,
        ChatMessage userMessage,
        ChatMessage assistantMessage,
        IReadOnlyList<ResolvedLink> links,
        CancellationToken cancellationToken
    )
    {
        var limit = TimeSpan.FromSeconds(_options.TurnLimitSeconds);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var prompt = _prompts.For(AgentRole.Chat);
        var run = new AgentRun(AgentRole.Chat, caseId: null, ingestAttemptId: null, now);
        run.Claim(now, now + limit);
        run.UseModel(_model.ModelName, prompt.Version, prompt.Hash);
        assistantMessage.AttachRun(run.Id);

        var throttle = new ChatSnapshotThrottle(
            _notifier,
            _timeProvider,
            TimeSpan.FromMilliseconds(_options.PushIntervalMilliseconds),
            chat.OwnerUserId,
            chat.Id,
            assistantMessage.Id);

        var counts = new TokenCounts();
        var buffer = new ChatToolCallBuffer(run.Id);

        using var limitSource = new CancellationTokenSource(limit, _timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, limitSource.Token);

        var ending = await ExecuteAsync(
            chat, userMessage, assistantMessage, links, prompt, (run, buffer), throttle, counts, limitSource,
            linked.Token);

        throttle.Flush();
        Apply(ending, chat, assistantMessage, run, throttle.Text, counts);

        if (ending != TurnEnding.NoAnswer)
        {
            _draftCommit.Stage(chat, assistantMessage, ending == TurnEnding.Completed);
        }

        await _saves.SaveAsync(run, buffer.Rows, CancellationToken.None);

        _notifier.AddChatTurnEnd(
            chat.OwnerUserId,
            new ChatTurnEnd
                { ChatId = chat.Id, MessageId = assistantMessage.Id, State = assistantMessage.State.Value });

        _notifier.Push();
    }

    private async Task<TurnEnding> ExecuteAsync(
        Chat chat,
        ChatMessage userMessage,
        ChatMessage assistantMessage,
        IReadOnlyList<ResolvedLink> links,
        AgentPrompt prompt,
        (AgentRun Run, ChatToolCallBuffer Buffer) toolRun,
        ChatSnapshotThrottle throttle,
        TokenCounts counts,
        CancellationTokenSource limitSource,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var labels = userMessage.Links.Select(link => link.Label).ToList();
            var answer = await _triage.AskAsync(userMessage.Text, labels, cancellationToken);
            counts.TriageInput = answer.InputTokens;
            counts.TriageOutput = answer.OutputTokens;

            if (answer.Verdict.Verdict == ChatTriageChoice.OffTopic)
            {
                return TurnEnding.Refusal;
            }

            if (answer.Verdict.Verdict == ChatTriageChoice.Malicious)
            {
                return TurnEnding.Blocked;
            }

            var history = await _historyBuilder.BuildAsync(
                chat, userMessage, assistantMessage, links, cancellationToken);

            var tools = BuildTools(chat, toolRun.Run, toolRun.Buffer, throttle);
            var isLengthLimited = false;

            await foreach (var chunk in _model.StreamAsync(prompt.Text, history, tools, cancellationToken))
            {
                counts.ModelInput = Math.Max(counts.ModelInput, chunk.InputTokens);
                counts.ModelOutput = Math.Max(counts.ModelOutput, chunk.OutputTokens);
                isLengthLimited |= chunk.IsLengthLimited;
                throttle.Append(chunk.Text);
                assistantMessage.Update(throttle.Text);
            }

            if (_state.IsNoAnswerReported)
            {
                return TurnEnding.NoAnswer;
            }

            if (string.IsNullOrWhiteSpace(throttle.Text))
            {
                return TurnEnding.ProviderUnavailable;
            }

            return isLengthLimited ? TurnEnding.OutputLimit : TurnEnding.Completed;
        }
        catch (Exception) when (limitSource.IsCancellationRequested)
        {
            return TurnEnding.TimeLimit;
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested)
        {
            return TurnEnding.Cancelled;
        }
        catch (ProviderUnavailableException)
        {
            return TurnEnding.ProviderUnavailable;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, message: "A chat turn ended with an unexpected error.");

            return TurnEnding.ProviderUnavailable;
        }
    }

    private ChatTurnTools BuildTools(Chat chat, AgentRun run, ChatToolCallBuffer buffer, ChatSnapshotThrottle throttle)
    {
        var context = new ToolContext
        {
            CompanyId = chat.CompanyId,
            RunId = run.Id,
            Recorder = buffer.Recorder,
            CurrentCallId = _toolCallIds.Current,
            EndLoop = _toolCallIds.EndLoop,
        };

        var functions = _registry.For(AgentRole.Chat, context, _permissions.Read(), _toolScopes);

        return ChatTurnTools.Create(functions, _options.MaxToolCalls, _toolCallIds.EndLoop, throttle.SetActivity);
    }

    private void Apply(
        TurnEnding ending,
        Chat chat,
        ChatMessage assistantMessage,
        AgentRun run,
        string text,
        TokenCounts counts
    )
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        switch (ending)
        {
            case TurnEnding.Completed:
                assistantMessage.Complete(ChatMessageKind.Answer, text);
                run.Complete(now, verdict: null, verdictReason: null, counts.Input, counts.Output);

                break;
            case TurnEnding.NoAnswer:
                assistantMessage.Complete(ChatMessageKind.Unanswerable, string.Empty);
                run.Complete(now, verdict: null, verdictReason: null, counts.Input, counts.Output);

                break;
            case TurnEnding.Refusal:
                assistantMessage.Complete(ChatMessageKind.Refusal, string.Empty);
                run.Complete(now, verdict: null, verdictReason: null, counts.Input, counts.Output);

                break;
            case TurnEnding.Blocked:
                assistantMessage.Complete(ChatMessageKind.BlockedNotice, string.Empty);
                chat.Block();
                run.Complete(now, verdict: null, verdictReason: null, counts.Input, counts.Output);

                break;
            case TurnEnding.Cancelled:
                assistantMessage.End(ChatMessageState.Cancelled, errorCode: null);
                run.Interrupt(now, counts.Input, counts.Output);

                break;
            case TurnEnding.TimeLimit:
                Fail(assistantMessage, run, AgentRunErrorCode.TimeLimit, now, counts);

                break;
            case TurnEnding.OutputLimit:
                Fail(assistantMessage, run, AgentRunErrorCode.OutputLimit, now, counts);

                break;
            default:
                Fail(assistantMessage, run, AgentRunErrorCode.ProviderUnavailable, now, counts);

                break;
        }
    }

    private static void Fail(
        ChatMessage assistantMessage,
        AgentRun run,
        AgentRunErrorCode errorCode,
        DateTime now,
        TokenCounts counts
    )
    {
        assistantMessage.End(ChatMessageState.Failed, errorCode);
        run.Fail(now, errorCode, counts.Input, counts.Output);
    }

    private enum TurnEnding
    {
        Completed,
        Refusal,
        Blocked,
        Cancelled,
        TimeLimit,
        OutputLimit,
        NoAnswer,
        ProviderUnavailable,
    }

    private sealed class TokenCounts
    {
        public int TriageInput { get; set; }

        public int TriageOutput { get; set; }

        public int ModelInput { get; set; }

        public int ModelOutput { get; set; }

        public int Input => TriageInput + ModelInput;

        public int Output => TriageOutput + ModelOutput;
    }
}
