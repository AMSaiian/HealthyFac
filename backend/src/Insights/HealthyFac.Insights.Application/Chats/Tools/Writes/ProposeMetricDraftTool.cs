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
public sealed record ProposeMetricDraftToolRequest
    : IAgentToolRequest<ProposeMetricDraftToolRequest, ProposeMetricArguments, ProposeMetricToolResponse>
{
    public const string ToolName = "propose_metric_draft";

    public const string ToolDescription =
        "Proposes one metric of a machine for the user to review: a measurable signal that the machine reports. The "
        + "machineId is an existing machine (from a tool result or a link) or the machineId that "
        + "propose_machine_draft returned in this turn. Send only values that you know. Limits and behavior are "
        + "optional.";

    /// <summary>The proposal as the extraction tool builds it, with the machine ID that the model gave.</summary>
    [JsonIgnore]
    public required ProposeMetricToolRequest Proposal { get; init; }

    public static ProposeMetricDraftToolRequest Create(ProposeMetricArguments arguments, ToolContext context) =>
        new() { Proposal = ProposeMetricToolRequest.Create(arguments, context) };
}

public sealed class ProposeMetricDraftToolPreHandlerCheck : PassThroughPreHandlerCheck<ProposeMetricDraftToolRequest>;

/// <summary>
/// Adds the proposal to the buffer of the turn through the extraction tool, so the extraction rules apply: the
/// validation, the repeat rule, and the count limit. The <c>machineId</c> is a create draft of this turn or an existing
/// machine that the user may read.
/// </summary>
/// <param name="targets">Finds the buffer of the machine.</param>
/// <param name="proposals">The extraction handler that validates and adds the proposal.</param>
public sealed class ProposeMetricDraftToolRequestHandler(
    ChatDraftTargets targets,
    IRequestHandler<ProposeMetricToolRequest, ProposeMetricToolResponse> proposals
) : IRequestHandler<ProposeMetricDraftToolRequest, ProposeMetricToolResponse>
{
    private readonly ChatDraftTargets _targets = targets;
    private readonly IRequestHandler<ProposeMetricToolRequest, ProposeMetricToolResponse> _proposals = proposals;

    public async Task<ProposeMetricToolResponse> HandleAsync(
        ProposeMetricDraftToolRequest request,
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
