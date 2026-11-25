using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Chats.Tools.Reads;

public sealed record GetPhaseTimesArguments
{
    public required Guid MachineId { get; init; }

    public required DateTime From { get; init; }

    public required DateTime To { get; init; }
}

[AgentTool(
    role: "chat",
    ToolName,
    Description = ToolDescription,
    IsRead = true,
    Permissions = [ChatToolPermissions.Telemetry.SignalsRead],
    Activity = "signals")]
public sealed record GetPhaseTimesToolRequest
    : IAgentToolRequest<GetPhaseTimesToolRequest, GetPhaseTimesArguments, GetPhaseTimesResponse>
{
    public const string ToolName = "get_phase_times";

    public const string ToolDescription =
        "Reads the seconds that a machine spent in each operating phase in a range, with the seconds in an "
        + "unknown mode and the seconds with no data. "
        + AgentToolTexts.UtcTimes;

    public required GetPhaseTimesArguments Arguments { get; init; }

    public static GetPhaseTimesToolRequest Create(GetPhaseTimesArguments arguments, ToolContext context) =>
        new() { Arguments = arguments };
}

public sealed record GetPhaseTimesResponse
{
    public required IReadOnlyList<ChatPhaseTime> Phases { get; init; }

    public required double UnknownModeSeconds { get; init; }

    public required double NoDataSeconds { get; init; }
}

public sealed record ChatPhaseTime
{
    public required string Code { get; init; }

    public required string Name { get; init; }

    public required bool IsTracked { get; init; }

    public required double Seconds { get; init; }
}

public sealed class GetPhaseTimesArgumentsValidator : AbstractValidator<GetPhaseTimesArguments>
{
    public GetPhaseTimesArgumentsValidator()
    {
        this.AddIdRule(arguments => arguments.MachineId);
        this.AddRangeRule(arguments => arguments.From, arguments => arguments.To);
    }
}

public sealed class GetPhaseTimesToolRequestValidator : AbstractValidator<GetPhaseTimesToolRequest>
{
    public GetPhaseTimesToolRequestValidator()
    {
        RuleFor(request => request.Arguments).SetValidator(new GetPhaseTimesArgumentsValidator());
    }
}

public sealed class GetPhaseTimesToolPreHandlerCheck : PassThroughPreHandlerCheck<GetPhaseTimesToolRequest>;

public sealed class GetPhaseTimesToolRequestHandler(IChatReads reads)
    : IRequestHandler<GetPhaseTimesToolRequest, GetPhaseTimesResponse>
{
    private readonly IChatReads _reads = reads;

    public Task<GetPhaseTimesResponse> HandleAsync(
        GetPhaseTimesToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        _reads.GetPhaseTimesAsync(request.Arguments, cancellationToken);
}
