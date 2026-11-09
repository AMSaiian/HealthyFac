using FluentValidation;
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

        public IRuleBuilderOptions<T, string?> IsFileDescription() =>
            ruleBuilder.MaximumLength(FileValidationConstants.Text.DescriptionMaxLength)
                .WithMessage(FileErrors.DescriptionLength)
                .MultilineText();
    }
}
