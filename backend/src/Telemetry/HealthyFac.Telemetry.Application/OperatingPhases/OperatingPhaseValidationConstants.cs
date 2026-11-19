using HealthyFac.Telemetry.Application.MetricDefinitions;
using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Telemetry.Application.OperatingPhases;

/// <summary>
/// Defines the validation limits of an operating phase. The code follows the metric key rule, and the name and the
/// description follow the metric definition limits, so one rule has one definition.
/// </summary>
public static class OperatingPhaseValidationConstants
{
    public const int CodeMaxLength = MetricDefinitionRules.KeyMaxLength;
    public const int NameMaxLength = MetricDefinitionValidationConstants.NameMaxLength;
    public const int DescriptionMaxLength = MetricDefinitionValidationConstants.DescriptionMaxLength;
}
