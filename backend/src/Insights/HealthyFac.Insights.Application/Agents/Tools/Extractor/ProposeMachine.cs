using System.Text.Json.Serialization;
using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Extraction.Proposals;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Agents.Tools.Extractor;

/// <summary>The arguments that the model gives <c>propose_machine</c>.</summary>
public sealed record ProposeMachineArguments
{
    public string? Name { get; init; }

    public string? SerialNumber { get; init; }

    public string? Description { get; init; }
}

[AgentTool(role: "extractor", ToolName, Description = ToolDescription)]
[LocalTool]
public sealed record ProposeMachineToolRequest
    : IAgentToolRequest<ProposeMachineToolRequest, ProposeMachineArguments, ProposeMachineToolResponse>
{
    public const string ToolName = "propose_machine";

    public const string ToolDescription =
        "Proposes one machine that the document describes. Call it before any other propose tool. The result holds the "
        + "machineId that the other propose tools need. A document that names no machine but lists metrics or "
        + "statuses needs one machine with every value empty.";

    [JsonIgnore]
    public ExtractionBuffer? Buffer { get; init; }

    /// <summary>
    /// The machine entry whose values this call sets, or <see langword="null"/> to add a machine. The chat uses it
    /// when a definition added the entry of an existing machine first.
    /// </summary>
    [JsonIgnore]
    public Guid? MachineDraftId { get; init; }

    public string? Name { get; init; }

    public string? SerialNumber { get; init; }

    public string? Description { get; init; }

    public static ProposeMachineToolRequest Create(ProposeMachineArguments arguments, ToolContext context) =>
        new()
        {
            Buffer = context.Extraction,
            Name = ProposalText.SingleLine(arguments.Name),
            SerialNumber = ProposalText.SingleLine(arguments.SerialNumber),
            Description = ProposalText.Description(arguments.Description),
        };
}

public sealed record ProposeMachineToolResponse
{
    public required Guid MachineId { get; init; }

    public required string Status { get; init; }
}

public class ProposeMachineToolRequestValidator : AbstractValidator<ProposeMachineToolRequest>
{
    public ProposeMachineToolRequestValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(request => request.Buffer).NotNull().WithMessage(ExtractionErrors.RunHasNoBuffer);

        RuleFor(request => request.Name).SingleLineText().MaximumLength(DraftMessageLimits.Machine.NameMaxLength);

        RuleFor(request => request.SerialNumber)
            .SingleLineText()
            .MaximumLength(DraftMessageLimits.Machine.SerialNumberMaxLength);

        RuleFor(request => request.Description).IsProposedDescription(richTextPolicy);
    }
}

public sealed class ProposeMachineToolPreHandlerCheck : PassThroughPreHandlerCheck<ProposeMachineToolRequest>;

/// <summary>
/// Adds the machine to the buffer of the run, or sets the values of the entry that the request names. The machine
/// file rule, the count limit, and the set-once rule reject the call to the model.
/// </summary>
public sealed class ProposeMachineToolRequestHandler
    : IRequestHandler<ProposeMachineToolRequest, ProposeMachineToolResponse>
{
    public Task<ProposeMachineToolResponse> HandleAsync(
        ProposeMachineToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var machineId = request.MachineDraftId is { } machineDraftId
            ? request.Buffer!.UpdateMachine(machineDraftId, request.Name, request.SerialNumber, request.Description)
            : request.Buffer!.AddMachine(request.Name, request.SerialNumber, request.Description);

        return Task.FromResult(
            new ProposeMachineToolResponse { MachineId = machineId, Status = ProposalToolStatus.Proposed });
    }
}
