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

/// <summary>
/// One condition of a phase as the model gives it. The condition names its source by the key of a metric or the code
/// of a status, never by an ID.
/// </summary>
public sealed record PhaseConditionArguments
{
    public string? MetricKey { get; init; }

    public string? StatusCode { get; init; }

    public string? Operator { get; init; }

    public double? Value { get; init; }

    public double? UpperValue { get; init; }

    public IReadOnlyList<string>? States { get; init; }
}

/// <summary>One limit row of a phase as the model gives it. The row names its metric by key.</summary>
public sealed record PhaseLimitArguments
{
    public string? MetricKey { get; init; }

    public double? WarningLow { get; init; }

    public double? WarningHigh { get; init; }

    public double? AlarmLow { get; init; }

    public double? AlarmHigh { get; init; }
}

/// <summary>The arguments that the model gives <c>propose_operating_phase</c>.</summary>
public sealed record ProposeOperatingPhaseArguments
{
    public required Guid MachineId { get; init; }

    public string? Code { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public int? Priority { get; init; }

    public bool? IsTracked { get; init; }

    public string? SourceLocation { get; init; }

    public IReadOnlyList<PhaseConditionArguments>? Conditions { get; init; }

    public IReadOnlyList<PhaseLimitArguments>? Limits { get; init; }
}

[AgentTool(role: "extractor", ToolName, Description = ToolDescription)]
[LocalTool]
public sealed record ProposeOperatingPhaseToolRequest
    : IAgentToolRequest<
        ProposeOperatingPhaseToolRequest,
        ProposeOperatingPhaseArguments,
        ProposeOperatingPhaseToolResponse>
{
    public const string ToolName = "propose_operating_phase";

    public const string ToolDescription =
        "Proposes one operating phase of a machine: a named state of work (for example idle or cutting) with the "
        + "conditions that tell it apart and the limits that hold in it. A condition or a limit row names its metric "
        + "by "
        + "metricKey and a status by statusCode. The key or the code must be proposed in this run (call propose_metric "
        + "or propose_status first) or be listed in the existing keys and codes of the machine.";

    [JsonIgnore]
    public ExtractionBuffer? Buffer { get; init; }

    public required Guid MachineId { get; init; }

    public string? SourceLocation { get; init; }

    public string? Code { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public int? Priority { get; init; }

    public bool? IsTracked { get; init; }

    public IReadOnlyList<PhaseConditionArguments> Conditions { get; init; } = [];

    public IReadOnlyList<PhaseLimitArguments> Limits { get; init; } = [];

    public static ProposeOperatingPhaseToolRequest Create(
        ProposeOperatingPhaseArguments arguments,
        ToolContext context
    ) =>
        new()
        {
            Buffer = context.Extraction,
            MachineId = arguments.MachineId,
            SourceLocation = ProposalText.Location(arguments.SourceLocation),
            Code = ProposalText.SingleLine(arguments.Code),
            Name = ProposalText.SingleLine(arguments.Name),
            Description = ProposalText.Description(arguments.Description),
            Priority = arguments.Priority,
            IsTracked = arguments.IsTracked,
            Conditions =
            [
                .. (arguments.Conditions ?? []).Select(condition => condition with
                {
                    MetricKey = ProposalText.SingleLine(condition.MetricKey),
                    StatusCode = ProposalText.SingleLine(condition.StatusCode),
                    Operator = ProposalText.SingleLine(condition.Operator),
                    Value = ProposalText.Finite(condition.Value),
                    UpperValue = ProposalText.Finite(condition.UpperValue),
                    States = ProposalText.States(condition.States),
                }),
            ],
            Limits =
            [
                .. (arguments.Limits ?? []).Select(limit => limit with
                {
                    MetricKey = ProposalText.SingleLine(limit.MetricKey),
                    WarningLow = ProposalText.Finite(limit.WarningLow),
                    WarningHigh = ProposalText.Finite(limit.WarningHigh),
                    AlarmLow = ProposalText.Finite(limit.AlarmLow),
                    AlarmHigh = ProposalText.Finite(limit.AlarmHigh),
                }),
            ],
        };
}

public sealed record ProposeOperatingPhaseToolResponse
{
    public required Guid Id { get; init; }

    public required string Status { get; init; }
}

public class ProposeOperatingPhaseToolRequestValidator : AbstractValidator<ProposeOperatingPhaseToolRequest>
{
    public ProposeOperatingPhaseToolRequestValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(request => request.Buffer).NotNull().WithMessage(ExtractionErrors.RunHasNoBuffer);

        RuleFor(request => request.MachineId).NotEmpty().WithMessage(AgentMessages.MachineIdRequired);

        RuleFor(request => request.SourceLocation)
            .SingleLineText()
            .MaximumLength(DraftMessageLimits.SourceLocationMaxLength);

        RuleFor(request => request.Code)
            .SingleLineText()
            .MaximumLength(DraftMessageLimits.Phase.CodeMaxLength)
            .Matches(DraftMessageLimits.KeyPattern)
            .WithMessage(DraftMessageErrors.Validation.KeyInvalid);

        RuleFor(request => request.Name).SingleLineText().MaximumLength(DraftMessageLimits.Phase.NameMaxLength);

        RuleFor(request => request.Description).IsProposedDescription(richTextPolicy);

        RuleFor(request => request.Priority)
            .GreaterThanOrEqualTo(valueToCompare: 0)
            .When(request => request.Priority is not null)
            .WithMessage(DraftMessageErrors.Validation.PriorityNegative);

        RuleForEach(request => request.Conditions).SetValidator(new PhaseConditionArgumentsValidator());
        RuleForEach(request => request.Limits).SetValidator(new PhaseLimitArgumentsValidator());
    }
}

public sealed class PhaseConditionArgumentsValidator : AbstractValidator<PhaseConditionArguments>
{
    public PhaseConditionArgumentsValidator()
    {
        RuleFor(condition => condition.MetricKey).SingleLineText();

        RuleFor(condition => condition.StatusCode).SingleLineText();

        RuleFor(condition => condition)
            .Must(condition => condition.MetricKey is null != condition.StatusCode is null)
            .WithMessage(DraftMessageErrors.Validation.ConditionSourceInvalid);

        RuleFor(condition => condition.Operator)
            .SingleLineText()
            .Must(conditionOperator => conditionOperator is null
                || DraftMessageLimits.PhaseOperators.All.Contains(conditionOperator))
            .WithMessage(DraftMessageErrors.Validation.OperatorInvalid);

        RuleForEach(condition => condition.States)
            .SingleLineText()
            .MaximumLength(DraftMessageLimits.Metric.StateMaxLength);
    }
}

public sealed class PhaseLimitArgumentsValidator : AbstractValidator<PhaseLimitArguments>
{
    public PhaseLimitArgumentsValidator()
    {
        RuleFor(limit => limit.MetricKey)
            .NotNull()
            .SingleLineText()
            .WithMessage(DraftMessageErrors.Validation.LimitMetricInvalid);

        RuleFor(limit => limit.WarningLow)
            .GreaterThanOrEqualTo(limit => limit.AlarmLow!.Value)
            .When(limit => limit.WarningLow is not null && limit.AlarmLow is not null)
            .WithMessage(DraftMessageErrors.Validation.WarningLowBelowAlarmLow);

        RuleFor(limit => limit.WarningHigh)
            .LessThanOrEqualTo(limit => limit.AlarmHigh!.Value)
            .When(limit => limit.WarningHigh is not null && limit.AlarmHigh is not null)
            .WithMessage(DraftMessageErrors.Validation.WarningHighAboveAlarmHigh);
    }
}

public sealed class ProposeOperatingPhaseToolPreHandlerCheck
    : PassThroughPreHandlerCheck<ProposeOperatingPhaseToolRequest>;

/// <summary>
/// Resolves the keys and the codes of the conditions and the limit rows, then adds the phase to the buffer. A key of
/// this run becomes a draft ID, a key of the machine becomes a definition ID, and any other key rejects the call to
/// the model.
/// </summary>
public sealed class ProposeOperatingPhaseToolRequestHandler
    : IRequestHandler<ProposeOperatingPhaseToolRequest, ProposeOperatingPhaseToolResponse>
{
    public Task<ProposeOperatingPhaseToolResponse> HandleAsync(
        ProposeOperatingPhaseToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var buffer = request.Buffer!;

        var phase = new ProposedPhase
        {
            DraftId = Guid.CreateVersion7(),
            SourceLocation = request.SourceLocation,
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            Priority = request.Priority,
            IsTracked = request.IsTracked,
            Conditions = [.. request.Conditions.Select(condition => ResolveCondition(buffer, request, condition))],
            Limits = [.. request.Limits.Select(limit => ResolveLimit(buffer, request, limit))],
        };

        buffer.AddPhase(request.MachineId, phase);

        return Task.FromResult(
            new ProposeOperatingPhaseToolResponse { Id = phase.DraftId, Status = ProposalToolStatus.Proposed });
    }

    private static ProposedPhaseCondition ResolveCondition(
        ExtractionBuffer buffer,
        ProposeOperatingPhaseToolRequest request,
        PhaseConditionArguments condition
    )
    {
        var metric = condition.MetricKey is { } key ? buffer.ResolveMetric(request.MachineId, key) : null;
        var status = condition.StatusCode is { } code ? buffer.ResolveStatus(request.MachineId, code) : null;

        return new ProposedPhaseCondition
        {
            MetricId = metric?.DefinitionId,
            MetricDraftId = metric?.DraftId,
            StatusId = status?.DefinitionId,
            StatusDraftId = status?.DraftId,
            Operator = condition.Operator,
            Value = condition.Value,
            UpperValue = condition.UpperValue,
            States = condition.States,
        };
    }

    private static ProposedPhaseLimit ResolveLimit(
        ExtractionBuffer buffer,
        ProposeOperatingPhaseToolRequest request,
        PhaseLimitArguments limit
    )
    {
        var metric = buffer.ResolveMetric(request.MachineId, limit.MetricKey!);

        return new ProposedPhaseLimit
        {
            MetricId = metric.DefinitionId,
            MetricDraftId = metric.DraftId,
            WarningLow = limit.WarningLow,
            WarningHigh = limit.WarningHigh,
            AlarmLow = limit.AlarmLow,
            AlarmHigh = limit.AlarmHigh,
        };
    }
}
