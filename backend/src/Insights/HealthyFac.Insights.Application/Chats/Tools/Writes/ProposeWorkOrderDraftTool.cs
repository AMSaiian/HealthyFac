using System.Text.Json.Serialization;
using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Chats.Drafts;
using HealthyFac.Insights.Application.Extraction.Proposals;
using HealthyFac.Insights.Messaging.WorkOrders;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Chats.Tools.Writes;

/// <summary>The arguments that the model gives <c>propose_work_order_draft</c>.</summary>
public sealed record ProposeWorkOrderDraftArguments
{
    /// <summary>The existing machine of the work order. Optional.</summary>
    public Guid? MachineId { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required string Priority { get; init; }

    public required string Category { get; init; }

    public required int EstimateMinutes { get; init; }
}

public sealed class ProposeWorkOrderDraftArgumentsValidator : AbstractValidator<ProposeWorkOrderDraftArguments>
{
    public ProposeWorkOrderDraftArgumentsValidator()
    {
        RuleFor(arguments => arguments.Title).NotEmpty().SingleLineText();

        RuleFor(arguments => arguments.Description!)
            .MultilineText()
            .When(arguments => arguments.Description is not null);

        RuleFor(arguments => arguments.Priority).NotEmpty().SingleLineText();
        RuleFor(arguments => arguments.Category).NotEmpty().SingleLineText();
    }
}

[AgentTool(
    role: "chat",
    ToolName,
    Description = ToolDescription,
    Permissions = [ChatToolPermissions.Core.DraftsWrite],
    Activity = "drafts")]
public sealed record ProposeWorkOrderDraftToolRequest
    : IAgentToolRequest<ProposeWorkOrderDraftToolRequest, ProposeWorkOrderDraftArguments, ProposeWorkOrderDraftResponse>
{
    public const string ToolName = "propose_work_order_draft";

    public const string ToolDescription =
        "Proposes a work order for the user to review. The priority is low, medium, high, or critical. The category "
        + "is inspection, preventive, repair, or other. The estimate is in minutes, in steps of 5 minutes. The "
        + "machineId is an existing machine and is optional.";

    public required ProposeWorkOrderDraftArguments Arguments { get; init; }

    private const string CandidateOwnerName = "Chat owner";

    /// <summary>The message as the save sends it, with placeholder chat IDs and name. It is checked only.</summary>
    [JsonIgnore]
    public required WorkOrderDraftProposed Candidate { get; init; }

    public static ProposeWorkOrderDraftToolRequest Create(
        ProposeWorkOrderDraftArguments arguments,
        ToolContext context
    ) =>
        new()
        {
            Arguments = arguments,
            Candidate = new WorkOrderDraftProposed
            {
                DraftId = Guid.CreateVersion7(),
                CompanyId = context.CompanyId,
                Source = WorkOrderProposalSource.Chat,
                ChatId = Guid.CreateVersion7(),
                ChatMessageId = Guid.CreateVersion7(),
                RequestedByName = CandidateOwnerName,
                MachineId = arguments.MachineId,
                Title = ProposalText.SingleLine(arguments.Title) ?? string.Empty,
                Description = arguments.Description,
                Priority = arguments.Priority,
                Category = arguments.Category,
                EstimateMinutes = arguments.EstimateMinutes,
            },
        };
}

public sealed record ProposeWorkOrderDraftResponse
{
    public required Guid Id { get; init; }

    public required string Status { get; init; }

    public string Note { get; init; } = DraftToolNotes.TellTheUser;
}

public sealed class ProposeWorkOrderDraftToolRequestValidator : AbstractValidator<ProposeWorkOrderDraftToolRequest>
{
    public ProposeWorkOrderDraftToolRequestValidator()
    {
        RuleFor(request => request.Arguments).SetValidator(new ProposeWorkOrderDraftArgumentsValidator());
        RuleFor(request => request.Candidate).SetValidator(new WorkOrderDraftProposedValidator());
    }
}

public sealed class
    ProposeWorkOrderDraftToolPreHandlerCheck : PassThroughPreHandlerCheck<ProposeWorkOrderDraftToolRequest>;

/// <summary>
/// Adds the work order to the buffer of the turn. The machine, when given, must be one that the user may read.
/// </summary>
/// <param name="targets">Checks the machine as the user.</param>
/// <param name="buffer">Holds the proposals of the turn.</param>
public sealed class ProposeWorkOrderDraftToolRequestHandler(ChatDraftTargets targets, ChatDraftBuffer buffer)
    : IRequestHandler<ProposeWorkOrderDraftToolRequest, ProposeWorkOrderDraftResponse>
{
    private readonly ChatDraftTargets _targets = targets;
    private readonly ChatDraftBuffer _buffer = buffer;

    public async Task<ProposeWorkOrderDraftResponse> HandleAsync(
        ProposeWorkOrderDraftToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var candidate = request.Candidate;

        if (candidate.MachineId is { } machineId)
        {
            await _targets.ResolveExistingAsync(machineId, cancellationToken);
        }

        _buffer.AddWorkOrder(
            new ChatWorkOrderProposal
            {
                DraftId = candidate.DraftId,
                MachineId = candidate.MachineId,
                Title = candidate.Title,
                Description = candidate.Description,
                Priority = candidate.Priority,
                Category = candidate.Category,
                EstimateMinutes = candidate.EstimateMinutes,
            });

        return new ProposeWorkOrderDraftResponse { Id = candidate.DraftId, Status = ProposalToolStatus.Proposed };
    }
}
