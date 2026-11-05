using FluentValidation;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Core.Application.Machines;

/// <summary>
/// Defines the stateless input rules of the machine fields. The create and the update validator call them, so each
/// rule has one definition.
/// </summary>
public static class MachineInputRules
{
    extension<T>(IRuleBuilder<T, string> ruleBuilder)
    {
        public IRuleBuilderOptions<T, string?> IsMachineName() =>
            ruleBuilder.NotEmpty()
                .WithMessage(MachineErrors.NameRequired)
                .MaximumLength(MachineValidationConstants.NameMaxLength)
                .SingleLineText();

        public IRuleBuilderOptions<T, string?> IsMachineSerialNumber() =>
            ruleBuilder.NotEmpty()
                .WithMessage(MachineErrors.SerialNumberRequired)
                .MaximumLength(MachineValidationConstants.SerialNumberMaxLength)
                .SingleLineText();
    }

    extension<T>(IRuleBuilder<T, string?> ruleBuilder)
    {
        public IRuleBuilderOptionsConditions<T, string?> IsMachineDescription(IRichTextPolicy richTextPolicy) =>
            ruleBuilder.IsRichText(
                richTextPolicy,
                MachineValidationConstants.DescriptionMaxLength,
                MachineErrors.DescriptionTooLong,
                MachineErrors.DescriptionNotAllowed,
                MachineErrors.DescriptionTooMuchFormatting
            );
    }
}
