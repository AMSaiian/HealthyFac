using FluentValidation;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Core.Application.WorkOrders;

/// <summary>
/// Defines the stateless input rules of the work order fields. The create and the update validator call them, so each
/// rule has one definition.
/// </summary>
public static class WorkOrderInputRules
{
    extension<T>(IRuleBuilder<T, string> ruleBuilder)
    {
        public IRuleBuilderOptions<T, string?> IsWorkOrderTitle() =>
            ruleBuilder.NotEmpty()
                .WithMessage(WorkOrderErrors.Validation.TitleRequired)
                .MaximumLength(WorkOrderValidationConstants.TitleMaxLength)
                .WithMessage(WorkOrderErrors.Validation.TitleTooLong)
                .SingleLineText();
    }

    extension<T>(IRuleBuilder<T, string?> ruleBuilder)
    {
        public IRuleBuilderOptionsConditions<T, string?> IsWorkOrderDescription(IRichTextPolicy richTextPolicy) =>
            ruleBuilder.IsRichText(
                richTextPolicy,
                WorkOrderValidationConstants.DescriptionMaxLength,
                WorkOrderErrors.Validation.DescriptionTooLong,
                WorkOrderErrors.Validation.DescriptionNotAllowed,
                WorkOrderErrors.Validation.DescriptionTooMuchFormatting);
    }

    extension<T>(IRuleBuilder<T, int> ruleBuilder)
    {
        public IRuleBuilderOptions<T, int> IsWorkOrderEstimate() =>
            ruleBuilder.InclusiveBetween(
                    WorkOrderValidationConstants.EstimateMinMinutes,
                    WorkOrderValidationConstants.EstimateMaxMinutes)
                .WithMessage(WorkOrderErrors.Validation.EstimateOutOfRange)
                .Must(minutes => minutes % WorkOrderValidationConstants.EstimateStepMinutes == 0)
                .WithMessage(WorkOrderErrors.Validation.EstimateOffGrid);
    }

    extension<T>(IRuleBuilder<T, IReadOnlyList<Guid>> ruleBuilder)
    {
        public IRuleBuilderOptions<T, IReadOnlyList<Guid>> IsWorkOrderFileList() =>
            ruleBuilder.NotNull()
                .WithMessage(WorkOrderErrors.Validation.FileIdsRequired)
                .Must(fileIds => fileIds.Count <= WorkOrderValidationConstants.FileMaxCount)
                .WithMessage(WorkOrderErrors.Validation.TooManyFiles)
                .Must(fileIds => fileIds.Distinct().Count() == fileIds.Count)
                .WithMessage(WorkOrderErrors.Validation.FileIdsDuplicate)
                .Must(fileIds => fileIds.All(fileId => fileId != Guid.Empty))
                .WithMessage(WorkOrderErrors.Validation.FileIdEmpty);
    }
}
