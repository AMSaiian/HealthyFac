using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Chats.Tools.Reads;

public sealed record GetWorkOrderArguments
{
    public required Guid WorkOrderId { get; init; }
}

[AgentTool(
    role: "chat",
    ToolName,
    Description = ToolDescription,
    IsRead = true,
    Permissions = [ChatToolPermissions.Core.WorkOrdersRead],
    Activity = "workOrders")]
public sealed record GetWorkOrderToolRequest
    : IAgentToolRequest<GetWorkOrderToolRequest, GetWorkOrderArguments, GetWorkOrderResponse>
{
    public const string ToolName = "get_work_order";

    public const string ToolDescription =
        "Reads one work order by its ID: the fields, the machine and assignee names, whether it is delayed, and "
        + "its newest history rows.";

    public required GetWorkOrderArguments Arguments { get; init; }

    public static GetWorkOrderToolRequest Create(GetWorkOrderArguments arguments, ToolContext context) =>
        new() { Arguments = arguments };
}

public sealed record GetWorkOrderResponse
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required string Status { get; init; }

    public required string Priority { get; init; }

    public required string Category { get; init; }

    public Guid? MachineId { get; init; }

    public string? MachineName { get; init; }

    public Guid? AssigneeId { get; init; }

    public string? AssigneeName { get; init; }

    public DateTime? PlannedStart { get; init; }

    public DateTime? PlannedEnd { get; init; }

    public required int EstimateMinutes { get; init; }

    public required bool IsDelayed { get; init; }

    public required DateTime CreatedAt { get; init; }

    public required IReadOnlyList<ChatWorkOrderHistoryRow> History { get; init; }
}

public sealed record ChatWorkOrderHistoryRow
{
    public required int Revision { get; init; }

    public required string Status { get; init; }

    public Guid? AssigneeId { get; init; }

    public string? AssigneeName { get; init; }

    public DateTime? PlannedStart { get; init; }

    public required int EstimateMinutes { get; init; }

    public required DateTime CreatedAt { get; init; }
}

public sealed class GetWorkOrderArgumentsValidator : AbstractValidator<GetWorkOrderArguments>
{
    public GetWorkOrderArgumentsValidator()
    {
        this.AddIdRule(arguments => arguments.WorkOrderId);
    }
}

public sealed class GetWorkOrderToolRequestValidator : AbstractValidator<GetWorkOrderToolRequest>
{
    public GetWorkOrderToolRequestValidator()
    {
        RuleFor(request => request.Arguments).SetValidator(new GetWorkOrderArgumentsValidator());
    }
}

public sealed class GetWorkOrderToolPreHandlerCheck : PassThroughPreHandlerCheck<GetWorkOrderToolRequest>;

public sealed class GetWorkOrderToolRequestHandler(IChatReads reads)
    : IRequestHandler<GetWorkOrderToolRequest, GetWorkOrderResponse>
{
    private readonly IChatReads _reads = reads;

    public Task<GetWorkOrderResponse> HandleAsync(
        GetWorkOrderToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        _reads.GetWorkOrderAsync(request.Arguments, cancellationToken);
}
