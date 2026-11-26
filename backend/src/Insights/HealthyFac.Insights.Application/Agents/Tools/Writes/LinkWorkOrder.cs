using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.WorkOrders;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Application.Agents.Tools.Writes;

/// <summary>The arguments that the model gives <c>link_work_order</c>.</summary>
public sealed record LinkWorkOrderArguments
{
    public required Guid WorkOrderId { get; init; }

    public required IReadOnlyList<Guid> EpisodeIds { get; init; }
}

[AgentTool(role: "planner", ToolName, Description = ToolDescription)]
[DownstreamScopes("work-orders")]
public sealed record LinkWorkOrderToolRequest
    : IAgentToolRequest<LinkWorkOrderToolRequest, LinkWorkOrderArguments, LinkWorkOrderToolResponse>
{
    public const string ToolName = "link_work_order";

    public const string ToolDescription =
        "Links open signals to an open work order of the machine that already covers them. The work order must be in "
        + "the list of list_open_work_orders. The link takes effect when the run ends.";

    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required Guid CaseId { get; init; }

    public required Guid RunId { get; init; }

    public required string CallId { get; init; }

    public required Guid WorkOrderId { get; init; }

    public required IReadOnlyList<Guid> EpisodeIds { get; init; }

    public static LinkWorkOrderToolRequest Create(LinkWorkOrderArguments arguments, ToolContext context) =>
        new()
        {
            CompanyId = context.CompanyId,
            MachineId = context.RequireMachineId(),
            CaseId = context.RequireCaseId(),
            RunId = context.RunId,
            CallId = context.CurrentCallId(),
            WorkOrderId = arguments.WorkOrderId,
            EpisodeIds = arguments.EpisodeIds,
        };
}

public sealed record LinkWorkOrderToolResponse
{
    public required Guid ActionId { get; init; }

    public required string Status { get; init; }
}

public class LinkWorkOrderToolRequestValidator : AbstractValidator<LinkWorkOrderToolRequest>
{
    public LinkWorkOrderToolRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(AgentMessages.CompanyIdRequired);

        RuleFor(request => request.MachineId).NotEmpty().WithMessage(AgentMessages.MachineIdRequired);

        RuleFor(request => request.CallId).NotEmpty().SingleLineText();

        RuleFor(request => request.WorkOrderId).NotEmpty().WithMessage(AgentMessages.WorkOrderIdRequired);

        RuleFor(request => request.EpisodeIds).NotEmpty().WithMessage(AgentMessages.EpisodeIdsRequired);

        RuleForEach(request => request.EpisodeIds).NotEmpty().WithMessage(AgentMessages.EpisodeIdsRequired);
    }
}

public sealed class LinkWorkOrderToolPreHandlerCheck(IInsightsDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<LinkWorkOrderToolRequest>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(LinkWorkOrderToolRequest request, CancellationToken cancellationToken) =>
        ToolMachineScope.EnsureAsync(_dbContext, _currentUser, request.CompanyId, request.MachineId, cancellationToken);
}

/// <summary>
/// Buffers a <c>linkWorkOrder</c> action. The work order must be one of the open work orders of the machine that
/// Core lists (the same list as <c>list_open_work_orders</c>), else the call is rejected to the model. The episodes
/// follow the rules of <see cref="ActionBuffering.EnsureEpisodesAsync"/>.
/// </summary>
/// <param name="dbContext">Reads the signals and saves the action.</param>
/// <param name="catalog">Reads the open work orders of the machine.</param>
/// <param name="options">Holds the number of work orders that the list reads.</param>
public sealed class LinkWorkOrderToolRequestHandler(
    IInsightsDbContext dbContext,
    IWorkOrderCatalog catalog,
    IOptions<AgentOptions> options
) : IRequestHandler<LinkWorkOrderToolRequest, LinkWorkOrderToolResponse>
{
    public const string WorkOrderNotOpenMessage =
        "The work order is not one of the open work orders of the machine.";

    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly IWorkOrderCatalog _catalog = catalog;
    private readonly AgentOptions _options = options.Value;

    public async Task<LinkWorkOrderToolResponse> HandleAsync(
        LinkWorkOrderToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var existingId = await ActionBuffering.FindAsync(_dbContext, request.RunId, request.CallId, cancellationToken);

        if (existingId is { } knownId)
        {
            return new LinkWorkOrderToolResponse
            {
                ActionId = knownId,
                Status = ProposeWorkOrderToolRequestHandler.BufferedStatus,
            };
        }

        var openWorkOrders = await _catalog.ListOpenAsync(
            request.MachineId,
            _options.OpenWorkOrderCount,
            cancellationToken);

        if (openWorkOrders.All(workOrder => workOrder.Id != request.WorkOrderId))
        {
            throw new UnprocessableException(WorkOrderNotOpenMessage);
        }

        await ActionBuffering.EnsureEpisodesAsync(
            _dbContext,
            request.CaseId,
            request.RunId,
            request.EpisodeIds,
            cancellationToken);

        var actionId = await ActionBuffering.AddAsync(
            _dbContext,
            request.RunId,
            request.CallId,
            AgentActionKind.LinkWorkOrder,
            ActionBuffering.Serialize(
                new LinkWorkOrderPayload
                {
                    SignalIds = [.. request.EpisodeIds.Distinct()],
                    WorkOrderId = request.WorkOrderId,
                }),
            cancellationToken);

        return new LinkWorkOrderToolResponse
        {
            ActionId = actionId,
            Status = ProposeWorkOrderToolRequestHandler.BufferedStatus,
        };
    }
}
