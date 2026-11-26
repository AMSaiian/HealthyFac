using System.Text.Json.Serialization;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Extractor;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Chats.Drafts;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Chats.Tools.Writes;

/// <summary>The arguments that the model gives <c>propose_machine_draft</c>.</summary>
public sealed record ProposeMachineDraftArguments
{
    /// <summary>The existing machine to update. Without it the draft creates a new machine.</summary>
    public Guid? TargetMachineId { get; init; }

    public string? Name { get; init; }

    public string? SerialNumber { get; init; }

    public string? Description { get; init; }
}

[AgentTool(
    role: "chat",
    ToolName,
    Description = ToolDescription,
    Permissions = [ChatToolPermissions.Core.DraftsWrite],
    Activity = "drafts")]
public sealed record ProposeMachineDraftToolRequest
    : IAgentToolRequest<ProposeMachineDraftToolRequest, ProposeMachineDraftArguments, ProposeMachineDraftResponse>
{
    public const string ToolName = "propose_machine_draft";

    public const string ToolDescription =
        "Proposes a machine for the user to review. Without targetMachineId it proposes a new machine. With "
        + "targetMachineId it proposes new values for that existing machine. The result holds the machineId that the "
        + "propose_metric_draft, propose_status_draft, and propose_operating_phase_draft tools take.";

    public Guid? TargetMachineId { get; init; }

    /// <summary>The proposal as the extraction tool builds it.</summary>
    [JsonIgnore]
    public required ProposeMachineToolRequest Proposal { get; init; }

    public static ProposeMachineDraftToolRequest Create(ProposeMachineDraftArguments arguments, ToolContext context) =>
        new()
        {
            TargetMachineId = arguments.TargetMachineId,
            Proposal = ProposeMachineToolRequest.Create(
                new ProposeMachineArguments
                {
                    Name = arguments.Name,
                    SerialNumber = arguments.SerialNumber,
                    Description = arguments.Description,
                },
                context),
        };
}

public sealed record ProposeMachineDraftResponse
{
    /// <summary>The ID that the later propose tools take as <c>machineId</c>.</summary>
    public required Guid MachineId { get; init; }

    public required string Status { get; init; }
}

public sealed class ProposeMachineDraftToolPreHandlerCheck : PassThroughPreHandlerCheck<ProposeMachineDraftToolRequest>;

/// <summary>
/// Adds a create draft or an update draft of a machine to the buffer of the turn through the extraction tool. The
/// update of an existing machine can come before or after the definitions of that machine, and a second update of the
/// same machine in one turn is rejected.
/// </summary>
/// <param name="targets">Finds the buffer of the machine.</param>
/// <param name="buffer">Holds the proposals of the turn.</param>
/// <param name="proposals">The extraction handler that validates and adds the machine.</param>
public sealed class ProposeMachineDraftToolRequestHandler(
    ChatDraftTargets targets,
    ChatDraftBuffer buffer,
    IRequestHandler<ProposeMachineToolRequest, ProposeMachineToolResponse> proposals
) : IRequestHandler<ProposeMachineDraftToolRequest, ProposeMachineDraftResponse>
{
    private readonly ChatDraftTargets _targets = targets;
    private readonly ChatDraftBuffer _buffer = buffer;
    private readonly IRequestHandler<ProposeMachineToolRequest, ProposeMachineToolResponse> _proposals = proposals;

    public async Task<ProposeMachineDraftResponse> HandleAsync(
        ProposeMachineDraftToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        if (request.TargetMachineId is not { } targetMachineId)
        {
            var created = await _targets.GetCreateBufferAsync(cancellationToken);
            var response = await _proposals.HandleAsync(request.Proposal with { Buffer = created }, cancellationToken);
            _buffer.RegisterCreateDraft(response.MachineId);

            return new ProposeMachineDraftResponse { MachineId = response.MachineId, Status = response.Status };
        }

        var existing = await _targets.ResolveExistingAsync(targetMachineId, cancellationToken);
        existing.ContainerId ??= existing.Buffer.AddMachine(name: null, serialNumber: null, description: null);

        var updated = await _proposals.HandleAsync(
            request.Proposal with { Buffer = existing.Buffer, MachineDraftId = existing.ContainerId },
            cancellationToken);

        return new ProposeMachineDraftResponse { MachineId = targetMachineId, Status = updated.Status };
    }
}
