using System.Linq.Expressions;
using System.Text.Json.Serialization;
using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Insights.Messaging.Drafts;

/// <summary>
/// Tells Core and Telemetry which machines, metrics, statuses, and phases a proposal holds. The source is the ingest of
/// one file or a chat message. Core stores the machine drafts; Telemetry stores the metric, status, and phase drafts.
/// A machine with a <see cref="ProposedMachine.TargetMachineId"/> updates that machine. A machine without one proposes
/// a new machine. Each definition names its machine: an existing one (<c>MachineId</c>) or a new one of the same
/// message (<c>MachineDraftId</c>).
/// </summary>
public sealed record DraftsProposed : IIntegrationMessage, IRequest
{
    public static string MessageType => "insights.v1:DraftsProposed";

    public required ProposalSource Source { get; init; }

    public required Guid CompanyId { get; init; }

    public Guid? AttemptId { get; init; }

    public Guid? FileId { get; init; }

    public bool? IsMachineFile { get; init; }

    /// <summary>The time when the user queued the ingest. It orders the proposal against later tie changes.</summary>
    public DateTime? QueuedAt { get; init; }

    public Guid? ChatId { get; init; }

    public Guid? ChatMessageId { get; init; }

    /// <summary>The display name of the chat owner. Required for the source chat, absent for the others.</summary>
    public string? RequestedByName { get; init; }

    public IReadOnlyList<ProposedMachine> Machines { get; init; } = [];

    public IReadOnlyList<ProposedMetric> Metrics { get; init; } = [];

    public IReadOnlyList<ProposedStatus> Statuses { get; init; } = [];

    public IReadOnlyList<ProposedPhase> Phases { get; init; } = [];
}

public sealed record ProposedMachine
{
    public required Guid DraftId { get; init; }

    public Guid? TargetMachineId { get; init; }

    public string? Name { get; init; }

    public string? SerialNumber { get; init; }

    public string? Description { get; init; }
}

public sealed record ProposedMetric
{
    public required Guid DraftId { get; init; }

    /// <summary>The existing machine of the definition. Exactly one of this and <c>MachineDraftId</c>.</summary>
    public Guid? MachineId { get; init; }

    /// <summary>The create draft of the same message that the definition belongs to.</summary>
    public Guid? MachineDraftId { get; init; }

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

    /// <summary>The existing machine of the definition. Exactly one of this and <c>MachineDraftId</c>.</summary>
    public Guid? MachineId { get; init; }

    /// <summary>The create draft of the same message that the definition belongs to.</summary>
    public Guid? MachineDraftId { get; init; }

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

    /// <summary>The existing machine of the definition. Exactly one of this and <c>MachineDraftId</c>.</summary>
    public Guid? MachineId { get; init; }

    /// <summary>The create draft of the same message that the definition belongs to.</summary>
    public Guid? MachineDraftId { get; init; }

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
        RuleFor(message => message.CompanyId).NotEmpty().WithMessage(DraftMessageErrors.Validation.IdRequired);
        RuleFor(message => message.Source).NotNull().WithMessage(DraftMessageErrors.Validation.SourceRequired);

        When(
            message => message.Source == ProposalSource.Ingest,
            () =>
            {
                RuleFor(message => message.AttemptId)
                    .MustBeSetId()
                    .WithMessage(DraftMessageErrors.Validation.IdRequired);

                RuleFor(message => message.FileId).MustBeSetId().WithMessage(DraftMessageErrors.Validation.IdRequired);

                RuleFor(message => message.IsMachineFile)
                    .NotNull()
                    .WithMessage(DraftMessageErrors.Validation.IsMachineFileRequired);

                RuleFor(message => message.QueuedAt)
                    .Must(queuedAt => queuedAt is { } value && value != default(DateTime))
                    .WithMessage(DraftMessageErrors.Validation.QueuedAtRequired);

                RuleFor(message => message.ChatId)
                    .Null()
                    .WithMessage(DraftMessageErrors.Validation.FieldsOfAnotherSource);

                RuleFor(message => message.ChatMessageId)
                    .Null()
                    .WithMessage(DraftMessageErrors.Validation.FieldsOfAnotherSource);

                RuleFor(message => message.RequestedByName)
                    .Null()
                    .WithMessage(DraftMessageErrors.Validation.FieldsOfAnotherSource);
            });

        When(
            message => message.Source == ProposalSource.Chat,
            () =>
            {
                RuleFor(message => message.ChatId).MustBeSetId().WithMessage(DraftMessageErrors.Validation.IdRequired);

                RuleFor(message => message.ChatMessageId)
                    .MustBeSetId()
                    .WithMessage(DraftMessageErrors.Validation.IdRequired);

                RuleFor(message => message.AttemptId)
                    .Null()
                    .WithMessage(DraftMessageErrors.Validation.FieldsOfAnotherSource);

                RuleFor(message => message.FileId)
                    .Null()
                    .WithMessage(DraftMessageErrors.Validation.FieldsOfAnotherSource);

                RuleFor(message => message.IsMachineFile)
                    .Null()
                    .WithMessage(DraftMessageErrors.Validation.FieldsOfAnotherSource);

                RuleFor(message => message.QueuedAt)
                    .Null()
                    .WithMessage(DraftMessageErrors.Validation.FieldsOfAnotherSource);

                RuleFor(message => message.RequestedByName)
                    .NotEmpty()
                    .WithMessage(DraftMessageErrors.Validation.RequestedByNameRequired)
                    .SingleLineText()
                    .MaximumLength(DraftMessageLimits.RequestedByNameMaxLength)
                    .WithMessage(DraftMessageErrors.Validation.RequestedByNameTooLong);
            });

        RuleFor(message => message.Machines.Count)
            .LessThanOrEqualTo(DraftMessageLimits.MaxMachines)
            .WithMessage(DraftMessageErrors.Validation.MachinesCount);

        RuleFor(message => message.Metrics)
            .Must(metrics => metrics.GroupBy(metric => metric.MachineId ?? metric.MachineDraftId)
                .All(group => group.Count() <= DraftMessageLimits.MaxMetrics))
            .WithMessage(DraftMessageErrors.Validation.MetricsCount);

        RuleFor(message => message.Statuses)
            .Must(statuses => statuses.GroupBy(status => status.MachineId ?? status.MachineDraftId)
                .All(group => group.Count() <= DraftMessageLimits.MaxStatuses))
            .WithMessage(DraftMessageErrors.Validation.StatusesCount);

        RuleFor(message => message)
            .Must(message => message.Machines.Count
                + message.Metrics.Count
                + message.Statuses.Count
                + message.Phases.Count
                > 0)
            .WithMessage(DraftMessageErrors.Validation.MessageWithoutContent);

        RuleFor(message => message.Machines)
            .Must(machines => machines is [] or [{ TargetMachineId: not null }])
            .When(message => message.IsMachineFile is true)
            .WithMessage(DraftMessageErrors.Validation.MachineFileNeedsOneTarget);

        RuleFor(message => message)
            .Must(HasOnlyCreateDraftsAsMachineDrafts)
            .WithMessage(DraftMessageErrors.Validation.MachineDraftUnknown);

        RuleFor(message => message)
            .Must(HasContentForEveryCreateDraft)
            .WithMessage(DraftMessageErrors.Validation.MachineWithoutContent);

        RuleForMachineOf(message => message.Metrics, metric => metric.MachineId, metric => metric.MachineDraftId);
        RuleForMachineOf(message => message.Statuses, status => status.MachineId, status => status.MachineDraftId);
        RuleForMachineOf(message => message.Phases, phase => phase.MachineId, phase => phase.MachineDraftId);

        RuleForEach(message => message.Machines).SetValidator(new ProposedMachineValidator(richTextPolicy));
        RuleForEach(message => message.Metrics).SetValidator(new ProposedMetricValidator(richTextPolicy));
        RuleForEach(message => message.Statuses).SetValidator(new ProposedStatusValidator(richTextPolicy));
        RuleForEach(message => message.Phases).SetValidator(new ProposedPhaseValidator(richTextPolicy));
    }

    private void RuleForMachineOf<TDefinition>(
        Expression<Func<DraftsProposed, IEnumerable<TDefinition>>> definitions,
        Func<TDefinition, Guid?> machineId,
        Func<TDefinition, Guid?> machineDraftId
    )
    {
        RuleForEach(definitions)
            .Must(definition => machineId(definition) is null != machineDraftId(definition) is null)
            .WithMessage(DraftMessageErrors.Validation.DefinitionMachineInvalid);

        RuleForEach(definitions)
            .Must(definition => machineId(definition) != Guid.Empty && machineDraftId(definition) != Guid.Empty)
            .WithMessage(DraftMessageErrors.Validation.IdRequired);
    }

    private static bool HasOnlyCreateDraftsAsMachineDrafts(DraftsProposed message)
    {
        var createDraftIds = message.Machines
            .Where(machine => machine.TargetMachineId is null)
            .Select(machine => machine.DraftId)
            .ToHashSet();

        return ReferencedMachineDraftIds(message)
            .Where(machineDraftId => machineDraftId != Guid.Empty)
            .All(createDraftIds.Contains);
    }

    private static bool HasContentForEveryCreateDraft(DraftsProposed message)
    {
        var referencedIds = ReferencedMachineDraftIds(message).ToHashSet();

        return message.Machines
            .Where(machine => machine.TargetMachineId is null)
            .All(machine => machine.Name is not null
                || machine.SerialNumber is not null
                || machine.Description is not null
                || referencedIds.Contains(machine.DraftId));
    }

    private static IEnumerable<Guid> ReferencedMachineDraftIds(DraftsProposed message) =>
        message.Metrics.Select(metric => metric.MachineDraftId)
            .Concat(message.Statuses.Select(status => status.MachineDraftId))
            .Concat(message.Phases.Select(phase => phase.MachineDraftId))
            .OfType<Guid>();
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

        RuleFor(machine => machine)
            .Must(machine => machine.Name is not null
                || machine.SerialNumber is not null
                || machine.Description is not null)
            .When(machine => machine.TargetMachineId is not null)
            .WithMessage(DraftMessageErrors.Validation.UpdateWithoutContent);
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
