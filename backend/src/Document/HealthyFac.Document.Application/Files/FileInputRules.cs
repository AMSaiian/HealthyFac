using FluentValidation;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Document.Application.Files;

/// <summary>
/// Defines the stateless input rules of the file fields. The upload and the update validators call them, so each rule
/// has one definition.
/// </summary>
public static class FileInputRules
{
    extension<T>(IRuleBuilder<T, string?> ruleBuilder)
    {
        public IRuleBuilderOptions<T, string?> IsFileNameInput() =>
            ruleBuilder.NotEmpty()
                .WithMessage(FileErrors.NameRequired)
                .MaximumLength(FileValidationConstants.Text.FileNameMaxLength)
                .WithMessage(FileErrors.NameTooLong)
                .SingleLineText();

        public IRuleBuilderOptionsConditions<T, string?> IsFileDescription(IRichTextPolicy richTextPolicy) =>
            ruleBuilder.IsRichText(
                richTextPolicy,
                FileValidationConstants.Text.DescriptionMaxLength,
                FileErrors.DescriptionTooLong,
                FileErrors.DescriptionNotAllowed,
                FileErrors.DescriptionTooMuchFormatting
            );
    }
}
