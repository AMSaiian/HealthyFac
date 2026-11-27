using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Chats.Tools;

public sealed record ReportNoAnswerArguments;

[AgentTool(role: "chat", ToolName, Description = ToolDescription)]
public sealed record ReportNoAnswerToolRequest
    : IAgentToolRequest<ReportNoAnswerToolRequest, ReportNoAnswerArguments, ReportNoAnswerResponse>
{
    public const string ToolName = "report_no_answer";

    public const string ToolDescription =
        "Call this tool when neither the data of the company nor general knowledge answers the question. It ends "
        + "the turn with no answer text.";

    public required Action EndLoop { get; init; }

    public static ReportNoAnswerToolRequest Create(ReportNoAnswerArguments arguments, ToolContext context) =>
        new() { EndLoop = context.EndLoop };
}

public sealed record ReportNoAnswerResponse
{
    public bool IsReported { get; init; } = true;
}

public sealed class ReportNoAnswerToolPreHandlerCheck : PassThroughPreHandlerCheck<ReportNoAnswerToolRequest>;

/// <summary>
/// Marks the turn as unanswerable and ends the function loop, so the model gets no further turn.
/// </summary>
/// <param name="state">Tells the turn runner that the model reported no answer.</param>
public sealed class ReportNoAnswerToolRequestHandler(ChatTurnState state)
    : IRequestHandler<ReportNoAnswerToolRequest, ReportNoAnswerResponse>
{
    private readonly ChatTurnState _state = state;

    public Task<ReportNoAnswerResponse> HandleAsync(
        ReportNoAnswerToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        _state.IsNoAnswerReported = true;
        request.EndLoop();

        return Task.FromResult(new ReportNoAnswerResponse());
    }
}
