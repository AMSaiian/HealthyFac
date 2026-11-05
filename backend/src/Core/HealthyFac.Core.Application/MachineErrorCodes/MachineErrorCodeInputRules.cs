using FluentValidation;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Core.Application.MachineErrorCodes;

/// <summary>
/// Defines the stateless input rules of the error code fields. The create and the update validator call them, so
/// each rule has one definition.
/// </summary>
public static class MachineErrorCodeInputRules
{
    extension<T>(IRuleBuilder<T, string> ruleBuilder)
    {
        public IRuleBuilderOptions<T, string?> IsMachineErrorCode() =>
            ruleBuilder.NotEmpty()
                .WithMessage(MachineErrorCodeErrors.CodeRequired)
                .MaximumLength(MachineErrorCodeValidationConstants.CodeMaxLength)
                .SingleLineText();
    }

    extension<T>(IRuleBuilder<T, string?> ruleBuilder)
    {
        public IRuleBuilderOptionsConditions<T, string?> IsMachineErrorCodeDescription(
            IRichTextPolicy richTextPolicy
        ) =>
            ruleBuilder.IsRichText(
                richTextPolicy,
                MachineErrorCodeValidationConstants.DescriptionMaxLength,
                MachineErrorCodeErrors.DescriptionTooLong,
                MachineErrorCodeErrors.DescriptionNotAllowed,
                MachineErrorCodeErrors.DescriptionTooMuchFormatting
            );
    }
}
