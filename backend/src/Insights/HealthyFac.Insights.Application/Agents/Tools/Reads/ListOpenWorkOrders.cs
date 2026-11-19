using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.WorkOrders;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Application.Agents.Tools.Reads;

/// <summary>The arguments of <c>list_open_work_orders</c>. The tool takes none.</summary>
public sealed record ListOpenWorkOrdersArguments;

[AgentTool(role: "planner", ToolName, Description = ToolDescription, IsRead = true)]
[DownstreamScopes("work-orders")]
public sealed record ListOpenWorkOrdersToolRequest
    : IAgentToolRequest<ListOpenWorkOrdersToolRequest, ListOpenWorkOrdersArguments, ListOpenWorkOrdersToolResponse>
{
    public const string ToolName = "list_open_work_orders";

    public const string ToolDescription =
        "Lists the open work orders of the machine (title, status, priority, category, planned times). Use it before "
        + "you propose a new work order, so you do not propose a duplicate.";

    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public static ListOpenWorkOrdersToolRequest Create(ListOpenWorkOrdersArguments arguments, ToolContext context) =>
        new() { CompanyId = context.CompanyId, MachineId = context.RequireMachineId() };
}

public sealed record ListOpenWorkOrdersToolResponse
{
    public required IReadOnlyList<OpenWorkOrderInfo> WorkOrders { get; init; }
}

public class ListOpenWorkOrdersToolRequestValidator : AbstractValidator<ListOpenWorkOrdersToolRequest>
{
    public ListOpenWorkOrdersToolRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(AgentMessages.CompanyIdRequired);

        RuleFor(request => request.MachineId).NotEmpty().WithMessage(AgentMessages.MachineIdRequired);
    }
}

public sealed class ListOpenWorkOrdersToolPreHandlerCheck(
    IInsightsDbContext dbContext,
    ICurrentUserProvider currentUser
)
    : IPreHandlerCheck<ListOpenWorkOrdersToolRequest>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(ListOpenWorkOrdersToolRequest request, CancellationToken cancellationToken) =>
        ToolMachineScope.EnsureAsync(_dbContext, _currentUser, request.CompanyId, request.MachineId, cancellationToken);
}

/// <summary>
/// Reads the open work orders of the machine from Core, at most <see cref="AgentOptions.OpenWorkOrderCount"/>. The
/// read holds no assignee.
/// </summary>
/// <param name="catalog">Reads the work orders.</param>
/// <param name="options">Holds the count.</param>
public sealed class ListOpenWorkOrdersToolRequestHandler(IWorkOrderCatalog catalog, IOptions<AgentOptions> options)
    : IRequestHandler<ListOpenWorkOrdersToolRequest, ListOpenWorkOrdersToolResponse>
{
    private readonly IWorkOrderCatalog _catalog = catalog;
    private readonly AgentOptions _options = options.Value;

    public async Task<ListOpenWorkOrdersToolResponse> HandleAsync(
        ListOpenWorkOrdersToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var workOrders = await _catalog.ListOpenAsync(
            request.MachineId,
            _options.OpenWorkOrderCount,
            cancellationToken);

        return new ListOpenWorkOrdersToolResponse { WorkOrders = workOrders };
    }
}
