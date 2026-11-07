using FluentValidation;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Telemetry.Application.MetricDefinitions;

/// <summary>
/// Defines the stateless input rules of the metric definition fields. The create and the update validator call them,
/// so each rule has one definition.
/// </summary>
public static class MetricDefinitionInputRules
{
    extension<T>(IRuleBuilder<T, string> ruleBuilder)
    {
        public IRuleBuilderOptions<T, string?> IsMetricKey() =>
            ruleBuilder.NotEmpty()
                .WithMessage(MetricDefinitionErrors.KeyRequired)
                .SingleLineText()
                .Must(MetricDefinitionRules.IsValidKey)
                .WithMessage(MetricDefinitionErrors.KeyInvalid);

        public IRuleBuilderOptions<T, string?> IsMetricName() =>
            ruleBuilder.NotEmpty()
                .WithMessage(MetricDefinitionErrors.NameRequired)
                .MaximumLength(MetricDefinitionValidationConstants.NameMaxLength)
                .SingleLineText();
    }

    extension<T>(IRuleBuilder<T, string?> ruleBuilder)
    {
        public IRuleBuilderOptionsConditions<T, string?> IsMetricDescription(IRichTextPolicy richTextPolicy) =>
            ruleBuilder.IsRichText(
                richTextPolicy,
                MetricDefinitionValidationConstants.DescriptionMaxLength,
                MetricDefinitionErrors.DescriptionTooLong,
                MetricDefinitionErrors.DescriptionNotAllowed,
                MetricDefinitionErrors.DescriptionTooMuchFormatting
            );
    }

    extension<T>(IRuleBuilderInitial<T, IReadOnlyList<string>?> ruleBuilder)
    {
        /// <summary>
        /// Checks the states of a state set when the list is present: at least the minimum count, every state a valid
        /// code, and no repeat. A list that is absent adds no failure here; the kind rule decides.
        /// </summary>
        public IRuleBuilderOptions<T, IReadOnlyList<string>?> HasValidStates() =>
            ruleBuilder.Must(states => states is null || states.Count >= MetricDefinitionRules.MinStateCount)
                .WithMessage(MetricDefinitionErrors.StatesTooFew)
                .Must(states => states is null || states.Distinct(StringComparer.Ordinal).Count() == states.Count)
                .WithMessage(MetricDefinitionErrors.StatesDuplicated)
                .Must(states => states is null || states.All(MetricDefinitionRules.IsValidKey))
                .WithMessage(MetricDefinitionErrors.StateInvalid);
    }
}
