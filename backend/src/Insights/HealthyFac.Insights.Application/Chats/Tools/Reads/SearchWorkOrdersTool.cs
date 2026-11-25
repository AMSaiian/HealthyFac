using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Chats.Tools.Reads;

public sealed record SearchWorkOrdersArguments
{
    public string[]? Statuses { get; init; }

    public Guid? AssigneeId { get; init; }

    public Guid? MachineId { get; init; }

    public DateTime? PlannedFrom { get; init; }

    public DateTime? PlannedTo { get; init; }

    public string? SortColumn { get; init; }

    public string? SortDirection { get; init; }

    public int? Limit { get; init; }

    public int? Offset { get; init; }
}

[AgentTool(
    role: "chat",
    ToolName,
    Description = ToolDescription,
    IsRead = true,
    Permissions = [ChatToolPermissions.Core.WorkOrdersRead],
    Activity = "workOrders")]
public sealed record SearchWorkOrdersToolRequest
    : IAgentToolRequest<SearchWorkOrdersToolRequest, SearchWorkOrdersArguments, SearchWorkOrdersResponse>
{
    public const string ToolName = "search_work_orders";

    public const string ToolDescription =
        "Searches the work orders that the user may read by status (for example inProgress), assignee, machine, "
        + "and planned start range. Sort by title, priority, plannedStart, createdAt, or isDelayed. Use limit and "
        + "offset to page. "
        + AgentToolTexts.UtcTimes;

    public required SearchWorkOrdersArguments Arguments { get; init; }

    public static SearchWorkOrdersToolRequest Create(SearchWorkOrdersArguments arguments, ToolContext context) =>
        new() { Arguments = arguments };
}

public sealed record SearchWorkOrdersResponse
{
    public required int TotalCount { get; init; }

    public required IReadOnlyList<ChatWorkOrderRow> WorkOrders { get; init; }
}

public sealed record ChatWorkOrderRow
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required string Status { get; init; }

    public required string Priority { get; init; }

    public required string Category { get; init; }

    public Guid? MachineId { get; init; }

    public Guid? AssigneeId { get; init; }

    public string? AssigneeName { get; init; }

    public DateTime? PlannedStart { get; init; }

    public DateTime? PlannedEnd { get; init; }

    public required int EstimateMinutes { get; init; }
}

public sealed class SearchWorkOrdersArgumentsValidator : AbstractValidator<SearchWorkOrdersArguments>
{
    public SearchWorkOrdersArgumentsValidator()
    {
        RuleFor(arguments => arguments.SortColumn!)
            .SingleLineText()
            .When(arguments => arguments.SortColumn is not null)
            .WithMessage(ChatToolErrors.TextHasOneLine);

        RuleFor(arguments => arguments.SortDirection!)
            .SingleLineText()
            .When(arguments => arguments.SortDirection is not null)
            .WithMessage(ChatToolErrors.TextHasOneLine);

        this.AddPagingRules(arguments => arguments.Limit, arguments => arguments.Offset);

        RuleForEach(arguments => arguments.Statuses!)
            .SingleLineText()
            .When(arguments => arguments.Statuses is not null)
            .WithMessage(ChatToolErrors.StatesInvalid);
    }
}

public sealed class SearchWorkOrdersToolRequestValidator : AbstractValidator<SearchWorkOrdersToolRequest>
{
    public SearchWorkOrdersToolRequestValidator()
    {
        RuleFor(request => request.Arguments).SetValidator(new SearchWorkOrdersArgumentsValidator());
    }
}

public sealed class SearchWorkOrdersToolPreHandlerCheck : PassThroughPreHandlerCheck<SearchWorkOrdersToolRequest>;

public sealed class SearchWorkOrdersToolRequestHandler(IChatReads reads)
    : IRequestHandler<SearchWorkOrdersToolRequest, SearchWorkOrdersResponse>
{
    private readonly IChatReads _reads = reads;

    public Task<SearchWorkOrdersResponse> HandleAsync(
        SearchWorkOrdersToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        _reads.SearchWorkOrdersAsync(request.Arguments, cancellationToken);
}
