using FluentValidation;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Telemetry.Domain.StatusDefinitions;

namespace HealthyFac.Telemetry.Application.StatusDefinitions;

/// <summary>
/// Defines the stateless input rules of the status definition fields. The create and the update validator call them, so
/// each rule has one definition.
/// </summary>
public static class StatusDefinitionInputRules
{
    extension<T>(IRuleBuilder<T, string> ruleBuilder)
    {
        public IRuleBuilderOptions<T, string?> IsStatusCode() =>
            ruleBuilder.NotEmpty()
                .WithMessage(StatusDefinitionErrors.CodeRequired)
                .MaximumLength(StatusDefinitionValidationConstants.CodeMaxLength)
                .SingleLineText();
    }

    extension<T>(IRuleBuilder<T, StatusSeverity?> ruleBuilder)
    {
        public IRuleBuilderOptions<T, StatusSeverity?> IsStatusSeverity() =>
            ruleBuilder.NotNull().WithMessage(StatusDefinitionErrors.SeverityRequired);
    }

    extension<T>(IRuleBuilder<T, string?> ruleBuilder)
    {
        public IRuleBuilderOptionsConditions<T, string?> IsStatusDefinitionDescription(
            IRichTextPolicy richTextPolicy
        ) =>
            ruleBuilder.IsRichText(
                richTextPolicy,
                StatusDefinitionValidationConstants.DescriptionMaxLength,
                StatusDefinitionErrors.DescriptionTooLong,
                StatusDefinitionErrors.DescriptionNotAllowed,
                StatusDefinitionErrors.DescriptionTooMuchFormatting
            );
    }
}
