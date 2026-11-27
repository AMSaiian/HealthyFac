using HealthyFac.Insights.Application.Agents.Tools;
using Microsoft.Extensions.AI;

namespace HealthyFac.Insights.Application.Chats.Tools;

/// <summary>
/// The tools that the model of one chat turn may call. Each function counts its calls: a call above the limit never
/// runs and gets a fixed text, and the call at the limit ends the function loop, so the model gets one final turn
/// without tools. Each call sets the activity of the snapshots to the tool group.
/// </summary>
public sealed class ChatTurnTools
{
    public const string LimitReachedText = "The tool call limit of this turn is reached. Answer with what you have.";

    /// <summary>
    /// The last message of the request without tools. The provider answers with no text when the request ends with a
    /// tool result, so this message asks for the answer.
    /// </summary>
    public const string AnswerNowText =
        "Answer the question now with the data that the tools returned. Call no more tools.";

    private readonly int _maxCalls;
    private int _callCount;

    private ChatTurnTools(IReadOnlyList<AIFunction> functions, int maxCalls)
    {
        Functions = functions;
        _maxCalls = maxCalls;
    }

    public IReadOnlyList<AIFunction> Functions { get; private set; }

    /// <summary>
    /// Tells that the model called <c>report_no_answer</c>, which ends the turn with no more model call.
    /// </summary>
    public bool IsNoAnswerReported { get; private set; }

    /// <summary>Tells that the turn used every call that it may use.</summary>
    public bool IsLimitReached => _callCount >= _maxCalls;

    /// <summary>
    /// Wraps the functions with the call limit and the activity.
    /// </summary>
    /// <param name="functions">The functions that <see cref="ToolRegistry"/> built.</param>
    /// <param name="maxCalls">The most calls of one turn.</param>
    /// <param name="endLoop">Ends the function loop after the current call.</param>
    /// <param name="setActivity">Sets the activity of the next snapshot.</param>
    /// <returns>The tools of the turn.</returns>
    public static ChatTurnTools Create(
        IReadOnlyList<AIFunction> functions,
        int maxCalls,
        Action endLoop,
        Action<string> setActivity
    )
    {
        var tools = new ChatTurnTools([], maxCalls);

        tools.Functions =
        [
            .. functions
                .Select(function => (AIFunction)new LimitedFunction(function, tools, endLoop, setActivity)),
        ];

        return tools;
    }

    private sealed class LimitedFunction(
        AIFunction inner,
        ChatTurnTools tools,
        Action endLoop,
        Action<string> setActivity
    ) : DelegatingAIFunction(inner)
    {
        private readonly ChatTurnTools _tools = tools;
        private readonly Action _endLoop = endLoop;
        private readonly Action<string> _setActivity = setActivity;

        protected override async ValueTask<object?> InvokeCoreAsync(
            AIFunctionArguments arguments,
            CancellationToken cancellationToken
        )
        {
            if (_tools.IsLimitReached)
            {
                _endLoop();

                return LimitReachedText;
            }

            _tools._callCount++;

            if (AdditionalProperties.TryGetValue(AgentToolAttribute.ActivityProperty, out var activity)
                && activity is string text)
            {
                _setActivity(text);
            }

            var result = await base.InvokeCoreAsync(arguments, cancellationToken);

            if (Name == ReportNoAnswerToolRequest.ToolName)
            {
                _tools.IsNoAnswerReported = true;
            }

            if (_tools.IsLimitReached)
            {
                _endLoop();
            }

            return result;
        }
    }
}
