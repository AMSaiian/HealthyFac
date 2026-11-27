using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Chats.Tools.Reads;

public sealed record GetMachineStateTimesArguments
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
    Permissions = [ChatToolPermissions.Core.MachinesRead],
    Activity = "machines")]
public sealed record GetMachineStateTimesToolRequest
    : IAgentToolRequest<GetMachineStateTimesToolRequest, GetMachineStateTimesArguments, GetMachineStateTimesResponse>
{
    public const string ToolName = "get_machine_state_times";

    public const string ToolDescription =
        "Reads the seconds that a machine spent in each state in a range. The seconds before the machine existed "
        + "come as their own value. "
        + AgentToolTexts.UtcTimes;

    public required GetMachineStateTimesArguments Arguments { get; init; }

    public static GetMachineStateTimesToolRequest Create(
        GetMachineStateTimesArguments arguments,
        ToolContext context
    ) =>
        new() { Arguments = arguments };
}

public sealed record GetMachineStateTimesResponse
{
    public required IReadOnlyList<ChatStateTime> States { get; init; }

    public required double BeforeCreationSeconds { get; init; }
}

public sealed record ChatStateTime
{
    public required string State { get; init; }

    public required double Seconds { get; init; }
}

public sealed class GetMachineStateTimesArgumentsValidator : AbstractValidator<GetMachineStateTimesArguments>
{
    public GetMachineStateTimesArgumentsValidator()
    {
        this.AddIdRule(arguments => arguments.MachineId);
        this.AddRangeRule(arguments => arguments.From, arguments => arguments.To);
    }
}

public sealed class GetMachineStateTimesToolRequestValidator : AbstractValidator<GetMachineStateTimesToolRequest>
{
    public GetMachineStateTimesToolRequestValidator()
    {
        RuleFor(request => request.Arguments).SetValidator(new GetMachineStateTimesArgumentsValidator());
    }
}

public sealed class
    GetMachineStateTimesToolPreHandlerCheck : PassThroughPreHandlerCheck<GetMachineStateTimesToolRequest>;

public sealed class GetMachineStateTimesToolRequestHandler(IChatReads reads)
    : IRequestHandler<GetMachineStateTimesToolRequest, GetMachineStateTimesResponse>
{
    private readonly IChatReads _reads = reads;

    public Task<GetMachineStateTimesResponse> HandleAsync(
        GetMachineStateTimesToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        _reads.GetMachineStateTimesAsync(request.Arguments, cancellationToken);
}
