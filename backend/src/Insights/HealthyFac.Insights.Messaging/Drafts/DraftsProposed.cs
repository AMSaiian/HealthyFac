using System.Text.Json.Serialization;
using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Insights.Messaging.Drafts;

/// <summary>
/// Tells Core and Telemetry which machines, metrics, and statuses the extraction of one file proposed. Core stores the
/// machine drafts; Telemetry stores the metric and status drafts. A user reviews them. A machine with a
/// <see cref="ProposedMachine.TargetMachineId"/> updates that machine. A machine without one proposes a new machine.
/// </summary>
public sealed record DraftsProposed : IIntegrationMessage, IRequest
{
    public static string MessageType => "insights.v1:DraftsProposed";

    public required Guid AttemptId { get; init; }

    public required Guid FileId { get; init; }

    public required Guid CompanyId { get; init; }

    public required bool IsMachineFile { get; init; }

    /// <summary>The time when the user queued the ingest. It orders the proposal against later tie changes.</summary>
    public required DateTime QueuedAt { get; init; }

    public required IReadOnlyList<ProposedMachine> Machines { get; init; }
}

public sealed record ProposedMachine
{
    public required Guid DraftId { get; init; }

    public Guid? TargetMachineId { get; init; }

    public string? Name { get; init; }

    public string? SerialNumber { get; init; }

    public string? Description { get; init; }

    public required IReadOnlyList<ProposedMetric> Metrics { get; init; }

    public required IReadOnlyList<ProposedStatus> Statuses { get; init; }

    public IReadOnlyList<ProposedPhase> Phases { get; init; } = [];
}

public sealed record ProposedMetric
{
    public required Guid DraftId { get; init; }

    public string? SourceLocation { get; init; }

    public string? Key { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    /// <summary>One of <see cref="DraftMessageLimits.ValueKinds.All"/>.</summary>
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

    /// <summary>One of <see cref="DraftMessageLimits.Behaviors.All"/>. Missing means <c>level</c>.</summary>
    public string? Behavior { get; init; }

    /// <summary>Tells whether the proposal sets at least one limit.</summary>
    [JsonIgnore]
    public bool HasLimits => WarningLow is not null
        || WarningHigh is not null
        || AlarmLow is not null
        || AlarmHigh is not null;
}

/// <summary>
/// An operating phase of a machine. A condition names its source and a limit row names its metric either as a
/// definition that exists or as a draft of the same message (exactly one of the two).
/// </summary>
public sealed record ProposedPhase
{
    public required Guid DraftId { get; init; }

    public string? SourceLocation { get; init; }

    public string? Code { get; init; }

    public string? Name { get; init; }

    public string? Description { get; init; }

    public int? Priority { get; init; }

    public bool? IsTracked { get; init; }

    public IReadOnlyList<ProposedPhaseCondition> Conditions { get; init; } = [];

    public IReadOnlyList<ProposedPhaseLimit> Limits { get; init; } = [];
}

public sealed record ProposedPhaseCondition
{
    public Guid? MetricId { get; init; }

    public Guid? MetricDraftId { get; init; }

    public Guid? StatusId { get; init; }

    public Guid? StatusDraftId { get; init; }

    /// <summary>One of <see cref="DraftMessageLimits.PhaseOperators.All"/>.</summary>
    public string? Operator { get; init; }

    public double? Value { get; init; }

    public double? UpperValue { get; init; }

    public IReadOnlyList<string>? States { get; init; }

    /// <summary>Tells how many sources the condition names. A valid condition names exactly one.</summary>
    [JsonIgnore]
    public int SourceCount => new[] { MetricId, MetricDraftId, StatusId, StatusDraftId }.Count(id => id is not null);
}

public sealed record ProposedPhaseLimit
{
    public Guid? MetricId { get; init; }

    public Guid? MetricDraftId { get; init; }

    public double? WarningLow { get; init; }

    public double? WarningHigh { get; init; }

    public double? AlarmLow { get; init; }

    public double? AlarmHigh { get; init; }

    /// <summary>Tells how many metrics the row names. A valid row names exactly one.</summary>
    [JsonIgnore]
    public int MetricCount => new[] { MetricId, MetricDraftId }.Count(id => id is not null);
}

public sealed record ProposedStatus
{
    public required Guid DraftId { get; init; }

    public string? SourceLocation { get; init; }

    public string? Code { get; init; }

    /// <summary>One of <see cref="DraftMessageLimits.Severities.All"/>.</summary>
    public string? Severity { get; init; }

    public string? Description { get; init; }
}

public class DraftsProposedValidator : AbstractValidator<DraftsProposed>
{
    public DraftsProposedValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(message => message.AttemptId).NotEmpty().WithMessage(DraftMessageErrors.Validation.IdRequired);
        RuleFor(message => message.FileId).NotEmpty().WithMessage(DraftMessageErrors.Validation.IdRequired);
        RuleFor(message => message.CompanyId).NotEmpty().WithMessage(DraftMessageErrors.Validation.IdRequired);
        RuleFor(message => message.QueuedAt).NotEmpty().WithMessage(DraftMessageErrors.Validation.QueuedAtRequired);

        RuleFor(message => message.Machines)
            .Must(machines => machines.Count is >= DraftMessageLimits.MinMachines and <= DraftMessageLimits.MaxMachines)
            .WithMessage(DraftMessageErrors.Validation.MachinesCount);

        RuleFor(message => message.Machines)
            .Must(machines => machines is [{ TargetMachineId: not null }])
            .When(message => message.IsMachineFile)
            .WithMessage(DraftMessageErrors.Validation.MachineFileNeedsOneTarget);

        RuleForEach(message => message.Machines).SetValidator(new ProposedMachineValidator(richTextPolicy));
    }
}

public class ProposedMachineValidator : AbstractValidator<ProposedMachine>
{
    public ProposedMachineValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(machine => machine.DraftId).NotEmpty().WithMessage(DraftMessageErrors.Validation.IdRequired);

        RuleFor(machine => machine.TargetMachineId)
            .NotEqual(Guid.Empty)
            .When(machine => machine.TargetMachineId is not null)
            .WithMessage(DraftMessageErrors.Validation.TargetMachineIdInvalid);

        RuleFor(machine => machine.Name)
            .SingleLineText()
            .MaximumLength(DraftMessageLimits.Machine.NameMaxLength);

        RuleFor(machine => machine.SerialNumber)
            .SingleLineText()
            .MaximumLength(DraftMessageLimits.Machine.SerialNumberMaxLength);

        RuleFor(machine => machine.Description).IsProposedDescription(richTextPolicy);

        RuleFor(machine => machine.Metrics.Count)
            .LessThanOrEqualTo(DraftMessageLimits.MaxMetrics)
            .WithMessage(DraftMessageErrors.Validation.MetricsCount);

        RuleFor(machine => machine.Statuses.Count)
            .LessThanOrEqualTo(DraftMessageLimits.MaxStatuses)
            .WithMessage(DraftMessageErrors.Validation.StatusesCount);

        RuleFor(machine => machine)
            .Must(machine => machine.Name is not null
                || machine.SerialNumber is not null
                || machine.Description is not null
                || machine.Metrics.Count > 0
                || machine.Statuses.Count > 0
                || machine.Phases.Count > 0)
            .WithMessage(DraftMessageErrors.Validation.MachineWithoutContent);

        RuleForEach(machine => machine.Metrics).SetValidator(new ProposedMetricValidator(richTextPolicy));
        RuleForEach(machine => machine.Statuses).SetValidator(new ProposedStatusValidator(richTextPolicy));
        RuleForEach(machine => machine.Phases).SetValidator(new ProposedPhaseValidator(richTextPolicy));
    }
}

public class ProposedMetricValidator : AbstractValidator<ProposedMetric>
{
    public ProposedMetricValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(metric => metric.DraftId).NotEmpty().WithMessage(DraftMessageErrors.Validation.IdRequired);

        RuleFor(metric => metric.SourceLocation)
            .SingleLineText()
            .MaximumLength(DraftMessageLimits.SourceLocationMaxLength);

        RuleFor(metric => metric.Key)
            .SingleLineText()
            .MaximumLength(DraftMessageLimits.Metric.KeyMaxLength)
            .Matches(DraftMessageLimits.KeyPattern)
            .WithMessage(DraftMessageErrors.Validation.KeyInvalid);

        RuleFor(metric => metric.Name)
            .SingleLineText()
            .MaximumLength(DraftMessageLimits.Metric.NameMaxLength);

        RuleFor(metric => metric.Description).IsProposedDescription(richTextPolicy);

        RuleFor(metric => metric.ValueKind)
            .SingleLineText()
            .Must(kind => kind is null || DraftMessageLimits.ValueKinds.All.Contains(kind))
            .WithMessage(DraftMessageErrors.Validation.ValueKindInvalid);

        RuleFor(metric => metric.QuantityKind)
            .SingleLineText()
            .MaximumLength(DraftMessageLimits.Metric.UnitNameMaxLength);

        RuleFor(metric => metric.Unit)
            .SingleLineText()
            .MaximumLength(DraftMessageLimits.Metric.UnitNameMaxLength);

        RuleFor(metric => metric)
            .Must(metric => metric.Min < metric.Max)
            .When(metric => metric.Min is not null && metric.Max is not null)
            .WithMessage(DraftMessageErrors.Validation.RangeInvalid);

        RuleFor(metric => metric.WarningLow)
            .GreaterThanOrEqualTo(metric => metric.AlarmLow!.Value)
            .When(metric => metric.WarningLow is not null && metric.AlarmLow is not null)
            .WithMessage(DraftMessageErrors.Validation.WarningLowBelowAlarmLow);

        RuleFor(metric => metric.WarningHigh)
            .LessThanOrEqualTo(metric => metric.AlarmHigh!.Value)
            .When(metric => metric.WarningHigh is not null && metric.AlarmHigh is not null)
            .WithMessage(DraftMessageErrors.Validation.WarningHighAboveAlarmHigh);

        RuleFor(metric => metric)
            .Must(metric => !metric.HasLimits)
            .When(metric => metric.ValueKind is not null and not DraftMessageLimits.ValueKinds.Numeric)
            .WithMessage(DraftMessageErrors.Validation.LimitsOnlyForNumeric);

        RuleFor(metric => metric.Behavior)
            .SingleLineText()
            .Must(behavior => behavior is null || DraftMessageLimits.Behaviors.All.Contains(behavior))
            .WithMessage(DraftMessageErrors.Validation.BehaviorInvalid);

        RuleFor(metric => metric.States)
            .Must(states => states!.Distinct(StringComparer.OrdinalIgnoreCase).Count() >= DraftMessageLimits.MinStates)
            .When(metric => metric.States is not null)
            .WithMessage(DraftMessageErrors.Validation.StatesInvalid);

        RuleForEach(metric => metric.States)
            .SingleLineText()
            .MaximumLength(DraftMessageLimits.Metric.StateMaxLength);

        RuleFor(metric => metric)
            .Must(metric => metric.Key is not null
                || metric.Name is not null
                || metric.Description is not null
                || metric.ValueKind is not null
                || metric.QuantityKind is not null
                || metric.Unit is not null
                || metric.Min is not null
                || metric.Max is not null
                || metric.States is not null
                || metric.HasLimits
                || metric.Behavior is not null)
            .WithMessage(DraftMessageErrors.Validation.MetricWithoutContent);
    }
}

public class ProposedPhaseValidator : AbstractValidator<ProposedPhase>
{
    public ProposedPhaseValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(phase => phase.DraftId).NotEmpty().WithMessage(DraftMessageErrors.Validation.IdRequired);

        RuleFor(phase => phase.SourceLocation)
            .SingleLineText()
            .MaximumLength(DraftMessageLimits.SourceLocationMaxLength);

        RuleFor(phase => phase.Code)
            .SingleLineText()
            .MaximumLength(DraftMessageLimits.Phase.CodeMaxLength)
            .Matches(DraftMessageLimits.KeyPattern)
            .WithMessage(DraftMessageErrors.Validation.KeyInvalid);

        RuleFor(phase => phase.Name)
            .SingleLineText()
            .MaximumLength(DraftMessageLimits.Phase.NameMaxLength);

        RuleFor(phase => phase.Description).IsProposedDescription(richTextPolicy);

        RuleFor(phase => phase.Priority)
            .GreaterThanOrEqualTo(valueToCompare: 0)
            .When(phase => phase.Priority is not null)
            .WithMessage(DraftMessageErrors.Validation.PriorityNegative);

        RuleForEach(phase => phase.Conditions).SetValidator(new ProposedPhaseConditionValidator());
        RuleForEach(phase => phase.Limits).SetValidator(new ProposedPhaseLimitValidator());
    }
}

public class ProposedPhaseConditionValidator : AbstractValidator<ProposedPhaseCondition>
{
    public ProposedPhaseConditionValidator()
    {
        RuleFor(condition => condition.SourceCount)
            .Equal(toCompare: 1)
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

public class ProposedPhaseLimitValidator : AbstractValidator<ProposedPhaseLimit>
{
    public ProposedPhaseLimitValidator()
    {
        RuleFor(limit => limit.MetricCount)
            .Equal(toCompare: 1)
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

public class ProposedStatusValidator : AbstractValidator<ProposedStatus>
{
    public ProposedStatusValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(status => status.DraftId).NotEmpty().WithMessage(DraftMessageErrors.Validation.IdRequired);

        RuleFor(status => status.SourceLocation)
            .SingleLineText()
            .MaximumLength(DraftMessageLimits.SourceLocationMaxLength);

        RuleFor(status => status.Code)
            .SingleLineText()
            .MaximumLength(DraftMessageLimits.Status.CodeMaxLength);

        RuleFor(status => status.Severity)
            .SingleLineText()
            .Must(severity => severity is null || DraftMessageLimits.Severities.All.Contains(severity))
            .WithMessage(DraftMessageErrors.Validation.SeverityInvalid);

        RuleFor(status => status.Description).IsProposedDescription(richTextPolicy);

        RuleFor(status => status)
            .Must(status => status.Code is not null || status.Severity is not null || status.Description is not null)
            .WithMessage(DraftMessageErrors.Validation.StatusWithoutContent);
    }
}

internal static class ProposedDescriptionRules
{
    extension<T>(IRuleBuilder<T, string?> rule)
    {
        public IRuleBuilderOptionsConditions<T, string?> IsProposedDescription(IRichTextPolicy richTextPolicy) =>
            rule.IsRichText(
                richTextPolicy,
                DraftMessageLimits.DescriptionMaxLength,
                DraftMessageErrors.Validation.DescriptionTooLong,
                DraftMessageErrors.Validation.DescriptionNotAllowed,
                DraftMessageErrors.Validation.DescriptionTooMuchFormatting);
    }
}
