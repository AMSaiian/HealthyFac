using FluentValidation;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Telemetry.Domain.OperatingPhases;

namespace HealthyFac.Telemetry.Application.OperatingPhases;

/// <summary>
/// The body of a phase: the create and the update use case share it, so each rule has one definition.
/// </summary>
public record OperatingPhaseInput
{
    public required string Code { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required int Priority { get; init; }

    public required bool IsTracked { get; init; }

    public required IReadOnlyList<PhaseConditionInput> Conditions { get; init; }

    public required IReadOnlyList<PhaseLimitInput> Limits { get; init; }
}

/// <summary>
/// One condition of a phase. Exactly one of the two IDs is set.
/// </summary>
public record PhaseConditionInput
{
    public Guid? MetricDefinitionId { get; init; }

    public Guid? StatusDefinitionId { get; init; }

    public required PhaseConditionOperator? Operator { get; init; }

    public double? Value { get; init; }

    public double? UpperValue { get; init; }

    public IReadOnlyList<string>? States { get; init; }
}

/// <summary>
/// One limit row of a phase. An empty limit means no limit on that side.
/// </summary>
public record PhaseLimitInput
{
    public required Guid MetricDefinitionId { get; init; }

    public double? WarningLow { get; init; }

    public double? WarningHigh { get; init; }

    public double? AlarmLow { get; init; }

    public double? AlarmHigh { get; init; }
}

public class PhaseConditionInputValidator : AbstractValidator<PhaseConditionInput>
{
    public PhaseConditionInputValidator()
    {
        RuleFor(x => x)
            .Must(condition => condition.MetricDefinitionId.HasValue != condition.StatusDefinitionId.HasValue)
            .WithMessage(OperatingPhaseErrors.ConditionSourceInvalid);

        RuleFor(x => x.Operator)
            .NotNull()
            .WithMessage(OperatingPhaseErrors.OperatorRequired);

        RuleFor(x => x.Value)
            .NotNull()
            .WithMessage(OperatingPhaseErrors.ValueRequired)
            .When(x => x.Operator?.HasComparisonValue == true);

        RuleFor(x => x.UpperValue)
            .NotNull()
            .WithMessage(OperatingPhaseErrors.UpperValueRequired)
            .GreaterThan(x => x.Value)
            .WithMessage(OperatingPhaseErrors.ValuesNotAscending)
            .When(x => x.Operator == PhaseConditionOperator.Between);

        RuleFor(x => x.States)
            .NotEmpty()
            .WithMessage(OperatingPhaseErrors.StatesRequired)
            .When(x => x.Operator == PhaseConditionOperator.In);

        RuleForEach(x => x.States)
            .SingleLineText();
    }
}

public class PhaseLimitInputValidator : AbstractValidator<PhaseLimitInput>
{
    public PhaseLimitInputValidator()
    {
        RuleFor(x => x.MetricDefinitionId)
            .NotEmpty()
            .WithMessage(OperatingPhaseErrors.LimitMetricIdRequired);

        RuleFor(x => x.WarningLow)
            .GreaterThanOrEqualTo(x => x.AlarmLow!.Value)
            .WithMessage(OperatingPhaseErrors.WarningLowBelowAlarmLow)
            .When(x => x.WarningLow.HasValue && x.AlarmLow.HasValue);

        RuleFor(x => x.WarningHigh)
            .LessThanOrEqualTo(x => x.AlarmHigh!.Value)
            .WithMessage(OperatingPhaseErrors.WarningHighAboveAlarmHigh)
            .When(x => x.WarningHigh.HasValue && x.AlarmHigh.HasValue);
    }
}
