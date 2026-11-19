using System.Text.Json.Serialization;
using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Extraction.Proposals;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Agents.Tools.Extractor;

/// <summary>The arguments that the model gives <c>propose_metric</c>.</summary>
public sealed record ProposeMetricArguments
{
    public required Guid MachineId { get; init; }

    public string? Key { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public string? ValueKind { get; init; }

    public string? QuantityKind { get; init; }

    public string? Unit { get; init; }

    public double? Min { get; init; }

    public double? Max { get; init; }

    public IReadOnlyList<string>? States { get; init; }

    public double? WarningLow { get; init; }

    public double? WarningHigh { get; init; }

    public double? AlarmLow { get; init; }

    public double? AlarmHigh { get; init; }

    public string? Behavior { get; init; }

    public string? SourceLocation { get; init; }
}

[AgentTool(role: "extractor", ToolName, Description = ToolDescription)]
[LocalTool]
public sealed record ProposeMetricToolRequest
    : IAgentToolRequest<ProposeMetricToolRequest, ProposeMetricArguments, ProposeMetricToolResponse>
{
    public const string ToolName = "propose_metric";

    public const string ToolDescription =
        "Proposes one metric of a machine: a measurable signal that the machine reports. The machineId is the one that "
        + "propose_machine returned. Send only values that the document states. Limits and behavior are optional.";

    [JsonIgnore]
    public ExtractionBuffer? Buffer { get; init; }

    public required Guid MachineId { get; init; }

    [JsonIgnore]
    public required ProposedMetric Metric { get; init; }

    public static ProposeMetricToolRequest Create(ProposeMetricArguments arguments, ToolContext context) =>
        new()
        {
            Buffer = context.Extraction,
            MachineId = arguments.MachineId,
            Metric = new ProposedMetric
            {
                DraftId = Guid.CreateVersion7(),
                SourceLocation = ProposalText.Location(arguments.SourceLocation),
                Key = ProposalText.SingleLine(arguments.Key),
                Name = ProposalText.SingleLine(arguments.Name),
                Description = ProposalText.Description(arguments.Description),
                ValueKind = ProposalText.SingleLine(arguments.ValueKind),
                QuantityKind = ProposalText.SingleLine(arguments.QuantityKind),
                Unit = ProposalText.SingleLine(arguments.Unit),
                Min = ProposalText.Finite(arguments.Min),
                Max = ProposalText.Finite(arguments.Max),
                States = ProposalText.States(arguments.States),
                WarningLow = ProposalText.Finite(arguments.WarningLow),
                WarningHigh = ProposalText.Finite(arguments.WarningHigh),
                AlarmLow = ProposalText.Finite(arguments.AlarmLow),
                AlarmHigh = ProposalText.Finite(arguments.AlarmHigh),
                Behavior = ProposalText.SingleLine(arguments.Behavior),
            },
        };
}

public sealed record ProposeMetricToolResponse
{
    public required Guid Id { get; init; }

    public required string Status { get; init; }
}

public class ProposeMetricToolRequestValidator : AbstractValidator<ProposeMetricToolRequest>
{
    public ProposeMetricToolRequestValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(request => request.Buffer).NotNull().WithMessage(ExtractionErrors.RunHasNoBuffer);

        RuleFor(request => request.MachineId).NotEmpty().WithMessage(AgentMessages.MachineIdRequired);

        RuleFor(request => request.Metric).SetValidator(new ProposedMetricValidator(richTextPolicy));

        RuleFor(request => request.Metric)
            .Must(metric => metric.Min is null && metric.Max is null)
            .When(request => IsKindSetAndNot(request.Metric, DraftMessageLimits.ValueKinds.Numeric))
            .WithMessage(ExtractionErrors.RangeNeedsNumeric);

        RuleFor(request => request.Metric)
            .Must(metric => metric.States is null)
            .When(request => IsKindSetAndNot(request.Metric, DraftMessageLimits.ValueKinds.StateSet))
            .WithMessage(ExtractionErrors.StatesNeedStateSet);

        RuleFor(request => request.Metric)
            .Must(metric => metric.QuantityKind is null == metric.Unit is null)
            .WithMessage(ExtractionErrors.UnitNeedsBothValues);
    }

    private static bool IsKindSetAndNot(ProposedMetric metric, string kind) =>
        metric.ValueKind is not null && metric.ValueKind != kind;
}

public sealed class ProposeMetricToolPreHandlerCheck : PassThroughPreHandlerCheck<ProposeMetricToolRequest>;

/// <summary>
/// Adds the metric to the buffer of the run. A pair of quantity kind and unit must be in the unit catalog. The buffer
/// rejects an unknown machine, a repeated key, and a reached count limit.
/// </summary>
public sealed class ProposeMetricToolRequestHandler
    : IRequestHandler<ProposeMetricToolRequest, ProposeMetricToolResponse>
{
    public Task<ProposeMetricToolResponse> HandleAsync(
        ProposeMetricToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var buffer = request.Buffer!;
        var metric = request.Metric;
        if (metric is { QuantityKind: { } quantityKind, Unit: { } unitName })
        {
            var unit = buffer.FindUnit(quantityKind, unitName) ?? throw ExtractionErrors.UnknownUnit();
            metric = metric with { QuantityKind = unit.QuantityKind, Unit = unit.Name };
        }

        buffer.AddMetric(request.MachineId, metric);

        return Task.FromResult(
            new ProposeMetricToolResponse { Id = metric.DraftId, Status = ProposalToolStatus.Proposed });
    }
}
