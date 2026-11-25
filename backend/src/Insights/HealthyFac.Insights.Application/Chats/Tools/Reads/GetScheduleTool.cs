using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Chats.Tools.Reads;

public sealed record GetScheduleArguments
{
    public Guid? EmployeeId { get; init; }

    public required string Date { get; init; }
}

[AgentTool(
    role: "chat",
    ToolName,
    Description = ToolDescription,
    IsRead = true,
    Permissions = [ChatToolPermissions.Core.ShiftsRead],
    Activity = "schedule")]
public sealed record GetScheduleToolRequest
    : IAgentToolRequest<GetScheduleToolRequest, GetScheduleArguments, GetScheduleResponse>
{
    public const string ToolName = "get_schedule";

    public const string ToolDescription =
        "Reads the resolved day of one employee (give employeeId) or of the whole company (leave employeeId out). "
        + "The date has the form yyyy-MM-dd. A user without the company read permission reads only their own day.";

    public required GetScheduleArguments Arguments { get; init; }

    public static GetScheduleToolRequest Create(GetScheduleArguments arguments, ToolContext context) =>
        new() { Arguments = arguments };
}

public sealed record GetScheduleResponse
{
    public required string Date { get; init; }

    public required string TimeZoneId { get; init; }

    public required IReadOnlyList<ChatScheduleDay> Days { get; init; }
}

public sealed record ChatScheduleDay
{
    public Guid? EmployeeId { get; init; }

    public string? EmployeeName { get; init; }

    public string? Source { get; init; }

    public required IReadOnlyList<ChatScheduleBlock> Blocks { get; init; }

    public required IReadOnlyList<ChatScheduledWorkOrder> WorkOrders { get; init; }
}

public sealed record ChatScheduleBlock
{
    public required string StartLocal { get; init; }

    public required string EndLocal { get; init; }
}

public sealed record ChatScheduledWorkOrder
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required string Status { get; init; }

    public required string Priority { get; init; }

    public required string PlannedStartLocal { get; init; }

    public required int EstimateMinutes { get; init; }

    public required bool IsDelayed { get; init; }
}

public sealed class GetScheduleArgumentsValidator : AbstractValidator<GetScheduleArguments>
{
    public GetScheduleArgumentsValidator()
    {
        RuleFor(arguments => arguments.Date)
            .NotEmpty()
            .SingleLineText()
            .WithMessage(ChatToolErrors.TextHasOneLine);
    }
}

public sealed class GetScheduleToolRequestValidator : AbstractValidator<GetScheduleToolRequest>
{
    public GetScheduleToolRequestValidator()
    {
        RuleFor(request => request.Arguments).SetValidator(new GetScheduleArgumentsValidator());
    }
}

public sealed class GetScheduleToolPreHandlerCheck : PassThroughPreHandlerCheck<GetScheduleToolRequest>;

public sealed class GetScheduleToolRequestHandler(IChatReads reads)
    : IRequestHandler<GetScheduleToolRequest, GetScheduleResponse>
{
    private readonly IChatReads _reads = reads;

    public Task<GetScheduleResponse> HandleAsync(
        GetScheduleToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        _reads.GetScheduleAsync(request.Arguments, cancellationToken);
}
