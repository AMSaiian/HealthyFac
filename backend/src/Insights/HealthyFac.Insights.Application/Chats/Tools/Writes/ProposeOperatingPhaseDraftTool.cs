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
public sealed record ProposeOperatingPhaseDraftToolRequest
    : IAgentToolRequest<ProposeOperatingPhaseDraftToolRequest, ProposeOperatingPhaseArguments,
        ProposeOperatingPhaseToolResponse>
{
    public const string ToolName = "propose_operating_phase_draft";

    public const string ToolDescription =
        "Proposes one operating phase of a machine for the user to review: a named state of work with the conditions "
        + "that tell it apart and the limits that hold in it. A condition or a limit row names its metric by "
        + "metricKey and a status by statusCode. The key or the code must be proposed in this turn or exist on "
        + "the machine.";

    /// <summary>The proposal as the extraction tool builds it, with the machine ID that the model gave.</summary>
    [JsonIgnore]
    public required ProposeOperatingPhaseToolRequest Proposal { get; init; }

    public static ProposeOperatingPhaseDraftToolRequest Create(
        ProposeOperatingPhaseArguments arguments,
        ToolContext context
    ) =>
        new() { Proposal = ProposeOperatingPhaseToolRequest.Create(arguments, context) };
}

public sealed class
    ProposeOperatingPhaseDraftToolPreHandlerCheck : PassThroughPreHandlerCheck<ProposeOperatingPhaseDraftToolRequest>;

/// <summary>
/// Adds the proposal to the buffer of the turn through the extraction tool, so the extraction rules apply: the
/// validation, the repeat rule, and the count limit. The <c>machineId</c> is a create draft of this turn or an existing
/// machine that the user may read.
/// </summary>
/// <param name="targets">Finds the buffer of the machine.</param>
/// <param name="proposals">The extraction handler that validates and adds the proposal.</param>
public sealed class ProposeOperatingPhaseDraftToolRequestHandler(
    ChatDraftTargets targets,
    IRequestHandler<ProposeOperatingPhaseToolRequest, ProposeOperatingPhaseToolResponse> proposals
) : IRequestHandler<ProposeOperatingPhaseDraftToolRequest, ProposeOperatingPhaseToolResponse>
{
    private readonly ChatDraftTargets _targets = targets;

    private readonly IRequestHandler<ProposeOperatingPhaseToolRequest, ProposeOperatingPhaseToolResponse> _proposals =
        proposals;

    public async Task<ProposeOperatingPhaseToolResponse> HandleAsync(
        ProposeOperatingPhaseDraftToolRequest request,
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
