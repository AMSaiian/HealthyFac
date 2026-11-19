using System.Text.Json.Serialization;
using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Extraction.Proposals;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Agents.Tools.Extractor;

/// <summary>The arguments that the model gives <c>propose_status</c>.</summary>
public sealed record ProposeStatusArguments
{
    public required Guid MachineId { get; init; }

    public string? Code { get; init; }

    public string? Severity { get; init; }

    public string? Description { get; init; }

    public string? SourceLocation { get; init; }
}

[AgentTool(role: "extractor", ToolName, Description = ToolDescription)]
[LocalTool]
public sealed record ProposeStatusToolRequest
    : IAgentToolRequest<ProposeStatusToolRequest, ProposeStatusArguments, ProposeStatusToolResponse>
{
    public const string ToolName = "propose_status";

    public const string ToolDescription =
        "Proposes one status, error, alarm, or warning code of a machine. The machineId is the one that "
        + "propose_machine returned. Send only values that the document states.";

    [JsonIgnore]
    public ExtractionBuffer? Buffer { get; init; }

    public required Guid MachineId { get; init; }

    [JsonIgnore]
    public required ProposedStatus Status { get; init; }

    public static ProposeStatusToolRequest Create(ProposeStatusArguments arguments, ToolContext context) =>
        new()
        {
            Buffer = context.Extraction,
            MachineId = arguments.MachineId,
            Status = new ProposedStatus
            {
                DraftId = Guid.CreateVersion7(),
                SourceLocation = ProposalText.Location(arguments.SourceLocation),
                Code = ProposalText.SingleLine(arguments.Code),
                Severity = ProposalText.SingleLine(arguments.Severity),
                Description = ProposalText.Description(arguments.Description),
            },
        };
}

public sealed record ProposeStatusToolResponse
{
    public required Guid Id { get; init; }

    public required string Status { get; init; }
}

public class ProposeStatusToolRequestValidator : AbstractValidator<ProposeStatusToolRequest>
{
    public ProposeStatusToolRequestValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(request => request.Buffer).NotNull().WithMessage(ExtractionErrors.RunHasNoBuffer);

        RuleFor(request => request.MachineId).NotEmpty().WithMessage(AgentMessages.MachineIdRequired);

        RuleFor(request => request.Status).SetValidator(new ProposedStatusValidator(richTextPolicy));
    }
}

public sealed class ProposeStatusToolPreHandlerCheck : PassThroughPreHandlerCheck<ProposeStatusToolRequest>;

/// <summary>
/// Adds the status to the buffer of the run. The buffer rejects an unknown machine, a repeated code, and a reached
/// count limit.
/// </summary>
public sealed class ProposeStatusToolRequestHandler
    : IRequestHandler<ProposeStatusToolRequest, ProposeStatusToolResponse>
{
    public Task<ProposeStatusToolResponse> HandleAsync(
        ProposeStatusToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        request.Buffer!.AddStatus(request.MachineId, request.Status);

        return Task.FromResult(
            new ProposeStatusToolResponse { Id = request.Status.DraftId, Status = ProposalToolStatus.Proposed });
    }
}
