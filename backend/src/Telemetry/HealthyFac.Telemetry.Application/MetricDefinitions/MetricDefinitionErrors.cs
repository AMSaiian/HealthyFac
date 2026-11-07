using HealthyFac.Shared.Application.RichText;
using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Telemetry.Application.MetricDefinitions;

/// <summary>
/// Defines error messages for metric definition operations.
/// </summary>
public static class MetricDefinitionErrors
{
    public const string CompanyIdRequired = "CompanyId is required.";
    public const string MachineIdRequired = "MachineId is required.";
    public const string ExpectedRevisionInvalid = "ExpectedRevision must be 0 or higher.";
    public const string KeyRequired = "Metric key is required.";
    public const string NameRequired = "Metric name is required.";
    public const string ValueKindRequired = "Value kind is required.";
    public const string IdRequired = "Metric definition ID is required.";
    public const string UnitRequired = "A numeric metric needs a unit.";
    public const string UnitNotAllowed = "Only a numeric metric has a unit.";
    public const string UnitDiscreteNotAllowed = "A numeric metric cannot use a discrete unit.";
    public const string UnitQuantityRequired = "The quantity kind of the unit is required.";
    public const string UnitNameRequired = "The unit name is required.";
    public const string UnitUnknown = "The unit does not exist in the unit catalog.";
    public const string RangeNotAllowed = "Only a numeric metric has a range.";
    public const string RangeNotAscending = "The minimum must be below the maximum.";
    public const string StatesRequired = "A state set needs states.";
    public const string StatesNotAllowed = "Only a state set has states.";
    public const string StatesDuplicated = "The states must be unique.";
    public const string KeyDuplicatedInRequest = "A key must appear once in the request.";

    public const string IdDuplicatedInRequest =
        "A metric definition must appear once in the updated and deleted lists.";

    public const string DescriptionNotAllowed = "The description contains formatting that is not allowed.";
    public const string MetricNotFound = "The metric definition does not exist.";
    public const string KeyTaken = "A metric with this key already exists for this machine.";
    public const string RevisionStale = "The metric definitions changed since you loaded them. Reload and apply again.";

    public static readonly string KeyInvalid =
        $"The key must match {MetricDefinitionRules.KeyPattern} and have at most "
        + $"{MetricDefinitionRules.KeyMaxLength} characters.";

    public static readonly string StateInvalid =
        $"A state must match {MetricDefinitionRules.KeyPattern} and have at most "
        + $"{MetricDefinitionRules.KeyMaxLength} characters.";

    public static readonly string StatesTooFew =
        $"A state set needs at least {MetricDefinitionRules.MinStateCount} states.";

    public static readonly string DescriptionTooLong =
        $"The description must have at most {MetricDefinitionValidationConstants.DescriptionMaxLength} characters.";

    public static readonly string DescriptionTooMuchFormatting =
        $"The description formatting is too large. The stored HTML must have at most {RichTextConstants.HtmlMaxLength} "
        + "characters.";
}
