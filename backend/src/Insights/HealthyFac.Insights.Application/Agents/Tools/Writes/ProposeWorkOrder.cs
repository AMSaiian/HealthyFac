using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Messaging.WorkOrders;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Agents.Tools.Writes;

/// <summary>The arguments that the model gives <c>propose_work_order</c>.</summary>
public sealed record ProposeWorkOrderArguments
{
    public required IReadOnlyList<Guid> EpisodeIds { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required string Priority { get; init; }

    public required string Category { get; init; }

    public required int EstimateMinutes { get; init; }
}

[AgentTool(role: "planner", ToolName, Description = ToolDescription)]
[LocalTool]
public sealed record ProposeWorkOrderToolRequest
    : IAgentToolRequest<ProposeWorkOrderToolRequest, ProposeWorkOrderArguments, ProposeWorkOrderToolResponse>
{
    public const string ToolName = "propose_work_order";

    public const string ToolDescription =
        "Proposes a work order for the machine. The episode IDs name the open signals that the work order covers. "
        + "A person reviews the proposal. The proposal takes effect when the run ends.";

    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required Guid CaseId { get; init; }

    public required Guid RunId { get; init; }

    public required string CallId { get; init; }

    public required IReadOnlyList<Guid> EpisodeIds { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required string Priority { get; init; }

    public required string Category { get; init; }

    public required int EstimateMinutes { get; init; }

    public static ProposeWorkOrderToolRequest Create(ProposeWorkOrderArguments arguments, ToolContext context) =>
        new()
        {
            CompanyId = context.CompanyId,
            MachineId = context.RequireMachineId(),
            CaseId = context.RequireCaseId(),
            RunId = context.RunId,
            CallId = context.CurrentCallId(),
            EpisodeIds = arguments.EpisodeIds,
            Title = arguments.Title,
            Description = arguments.Description,
            Priority = arguments.Priority,
            Category = arguments.Category,
            EstimateMinutes = arguments.EstimateMinutes,
        };
}

public sealed record ProposeWorkOrderToolResponse
{
    public required Guid ActionId { get; init; }

    public required string Status { get; init; }
}

public class ProposeWorkOrderToolRequestValidator : AbstractValidator<ProposeWorkOrderToolRequest>
{
    public ProposeWorkOrderToolRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(AgentMessages.CompanyIdRequired);

        RuleFor(request => request.MachineId).NotEmpty().WithMessage(AgentMessages.MachineIdRequired);

        RuleFor(request => request.CallId).NotEmpty().SingleLineText();

        RuleFor(request => request.EpisodeIds).NotEmpty().WithMessage(AgentMessages.EpisodeIdsRequired);

        RuleForEach(request => request.EpisodeIds).NotEmpty().WithMessage(AgentMessages.EpisodeIdsRequired);

        RuleFor(request => request.Title)
            .NotEmpty()
            .SingleLineText()
            .MaximumLength(WorkOrderDraftMessageLimits.TitleMaxLength)
            .WithMessage(WorkOrderDraftMessageErrors.Validation.TitleTooLong);

        RuleFor(request => request.Description)
            .MultilineText()
            .MaximumLength(WorkOrderDraftMessageLimits.DescriptionMaxLength)
            .WithMessage(WorkOrderDraftMessageErrors.Validation.DescriptionTooLong);

        RuleFor(request => request.Priority)
            .SingleLineText()
            .Must(priority => WorkOrderDraftMessageLimits.Priorities.All.Contains(priority))
            .WithMessage(WorkOrderDraftMessageErrors.Validation.PriorityInvalid);

        RuleFor(request => request.Category)
            .SingleLineText()
            .Must(category => WorkOrderDraftMessageLimits.Categories.All.Contains(category))
            .WithMessage(WorkOrderDraftMessageErrors.Validation.CategoryInvalid);

        RuleFor(request => request.EstimateMinutes)
            .InclusiveBetween(
                WorkOrderDraftMessageLimits.EstimateMinMinutes,
                WorkOrderDraftMessageLimits.EstimateMaxMinutes)
            .Must(estimate => estimate % WorkOrderDraftMessageLimits.EstimateStepMinutes == 0)
            .WithMessage(WorkOrderDraftMessageErrors.Validation.EstimateInvalid);
    }
}

public sealed class ProposeWorkOrderToolPreHandlerCheck(IInsightsDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<ProposeWorkOrderToolRequest>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(ProposeWorkOrderToolRequest request, CancellationToken cancellationToken) =>
        ToolMachineScope.EnsureAsync(_dbContext, _currentUser, request.CompanyId, request.MachineId, cancellationToken);
}

/// <summary>
/// Buffers a <c>proposeWorkOrder</c> action. Each episode must be an open signal of the case that no draft, no work
/// order, and no earlier action of the run covers, else the call is rejected to the model and buffers nothing. A
/// repeated call (the same call ID) returns the action that the first call buffered.
/// </summary>
/// <param name="dbContext">Reads the signals and saves the action.</param>
public sealed class ProposeWorkOrderToolRequestHandler(IInsightsDbContext dbContext)
    : IRequestHandler<ProposeWorkOrderToolRequest, ProposeWorkOrderToolResponse>
{
    public const string BufferedStatus = "buffered";

    private readonly IInsightsDbContext _dbContext = dbContext;

    public async Task<ProposeWorkOrderToolResponse> HandleAsync(
        ProposeWorkOrderToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var existingId = await ActionBuffering.FindAsync(_dbContext, request.RunId, request.CallId, cancellationToken);

        if (existingId is { } knownId)
        {
            return new ProposeWorkOrderToolResponse { ActionId = knownId, Status = BufferedStatus };
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
            AgentActionKind.ProposeWorkOrder,
            ActionBuffering.Serialize(
                new ProposeWorkOrderPayload
                {
                    SignalIds = [.. request.EpisodeIds.Distinct()],
                    Title = request.Title,
                    Description = request.Description,
                    Priority = request.Priority,
                    Category = request.Category,
                    EstimateMinutes = request.EstimateMinutes,
                }),
            cancellationToken);

        return new ProposeWorkOrderToolResponse { ActionId = actionId, Status = BufferedStatus };
    }
}
