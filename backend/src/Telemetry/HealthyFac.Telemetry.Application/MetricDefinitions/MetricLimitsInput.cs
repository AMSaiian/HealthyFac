using FluentValidation;
using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Telemetry.Application.MetricDefinitions;

/// <summary>
/// The limits and the behavior that a created or an updated metric definition carries. An empty limit means no limit
/// on that side.
/// </summary>
public interface IMetricLimitsInput
{
    double? WarningLow { get; }

    double? WarningHigh { get; }

    double? AlarmLow { get; }

    double? AlarmHigh { get; }

    MetricBehavior? Behavior { get; }
}

/// <summary>
/// Defines the stateless limit rules of a metric definition body. The create and the update validator call them, so
/// each rule has one definition. The rule that only a numeric metric has limits needs the stored kind, so each caller
/// adds it.
/// </summary>
public static class MetricLimitsInputRules
{
    extension<T>(AbstractValidator<T> validator)
        where T : IMetricLimitsInput
    {
        /// <summary>
        /// Adds the order rules of the limits to a validator: a warning limit stays inside its alarm limit.
        /// </summary>
        public void AddMetricLimitRules()
        {
            validator.RuleFor(x => x.WarningLow)
                .GreaterThanOrEqualTo(x => x.AlarmLow!.Value)
                .WithMessage(MetricDefinitionErrors.WarningLowBelowAlarmLow)
                .When(x => x.WarningLow.HasValue && x.AlarmLow.HasValue);

            validator.RuleFor(x => x.WarningHigh)
                .LessThanOrEqualTo(x => x.AlarmHigh!.Value)
                .WithMessage(MetricDefinitionErrors.WarningHighAboveAlarmHigh)
                .When(x => x.WarningHigh.HasValue && x.AlarmHigh.HasValue);
        }
    }
}

/// <summary>
/// Converts the limit fields of a request body to the domain value.
/// </summary>
public static class MetricLimitsInputExtensions
{
    extension(IMetricLimitsInput input)
    {
        /// <summary>
        /// Builds the limits of the body.
        /// </summary>
        /// <returns>The limits of the body.</returns>
        public MetricLimits ToLimits() =>
            new()
            {
                WarningLow = input.WarningLow,
                WarningHigh = input.WarningHigh,
                AlarmLow = input.AlarmLow,
                AlarmHigh = input.AlarmHigh,
            };

        /// <summary>
        /// Tells whether the body sets at least one limit.
        /// </summary>
        public bool HasAnyLimit => input.ToLimits().HasAny;
    }
}
