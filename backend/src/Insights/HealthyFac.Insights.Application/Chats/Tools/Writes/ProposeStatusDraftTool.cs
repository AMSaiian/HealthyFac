using System.Text.Json.Serialization;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Extractor;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Chats.Drafts;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Chats.Tools.Writes;

[AgentTool(
    role: "chat",
    ToolName,
    Description = ToolDescription,
    Permissions = [ChatToolPermissions.Telemetry.DefinitionDraftsWrite],
    Activity = "drafts")]
public sealed record ProposeStatusDraftToolRequest
    : IAgentToolRequest<ProposeStatusDraftToolRequest, ProposeStatusArguments, ProposeStatusToolResponse>
{
    public const string ToolName = "propose_status_draft";

    public const string ToolDescription =
        "Proposes one status of a machine for the user to review. The machineId is an existing machine (from a tool "
        + "result or a link) or the machineId that propose_machine_draft returned in this turn.";

    /// <summary>The proposal as the extraction tool builds it, with the machine ID that the model gave.</summary>
    [JsonIgnore]
    public required ProposeStatusToolRequest Proposal { get; init; }

    public static ProposeStatusDraftToolRequest Create(ProposeStatusArguments arguments, ToolContext context) =>
        new() { Proposal = ProposeStatusToolRequest.Create(arguments, context) };
}

public sealed class ProposeStatusDraftToolPreHandlerCheck : PassThroughPreHandlerCheck<ProposeStatusDraftToolRequest>;

/// <summary>
/// Adds the proposal to the buffer of the turn through the extraction tool, so the extraction rules apply: the
/// validation, the repeat rule, and the count limit. The <c>machineId</c> is a create draft of this turn or an existing
/// machine that the user may read.
/// </summary>
/// <param name="targets">Finds the buffer of the machine.</param>
/// <param name="proposals">The extraction handler that validates and adds the proposal.</param>
public sealed class ProposeStatusDraftToolRequestHandler(
    ChatDraftTargets targets,
    IRequestHandler<ProposeStatusToolRequest, ProposeStatusToolResponse> proposals
) : IRequestHandler<ProposeStatusDraftToolRequest, ProposeStatusToolResponse>
{
    private readonly ChatDraftTargets _targets = targets;
    private readonly IRequestHandler<ProposeStatusToolRequest, ProposeStatusToolResponse> _proposals = proposals;

    public async Task<ProposeStatusToolResponse> HandleAsync(
        ProposeStatusDraftToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var (buffer, machineKey) = await _targets.ResolveAsync(request.Proposal.MachineId, cancellationToken);

        var response = await _proposals.HandleAsync(
            request.Proposal with { Buffer = buffer, MachineId = machineKey },
            cancellationToken);

        return response with { Note = DraftToolNotes.TellTheUser };
    }
}
