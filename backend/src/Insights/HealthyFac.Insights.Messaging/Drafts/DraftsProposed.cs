using FluentValidation;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Insights.Messaging.Drafts;

/// <summary>
/// Tells Core which machines, metrics, and statuses the extraction of one file proposed. Core stores them as drafts
/// that a user reviews. A machine with a <see cref="ProposedMachine.TargetMachineId"/> updates that machine. A machine
/// without one proposes a new machine.
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
                || machine.Statuses.Count > 0)
            .WithMessage(DraftMessageErrors.Validation.MachineWithoutContent);

        RuleForEach(machine => machine.Metrics).SetValidator(new ProposedMetricValidator(richTextPolicy));
        RuleForEach(machine => machine.Statuses).SetValidator(new ProposedStatusValidator(richTextPolicy));
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
                || metric.States is not null)
            .WithMessage(DraftMessageErrors.Validation.MetricWithoutContent);
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
