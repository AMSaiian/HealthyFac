using FluentValidation;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Telemetry.Application.OperatingPhases;

/// <summary>
/// Defines the stateless input rules of the phase body. The create and the update validator call it, so each rule has
/// one definition and stays visible in the validator of its request.
/// </summary>
public static class OperatingPhaseInputRules
{
    extension<T>(AbstractValidator<T> validator)
        where T : OperatingPhaseInput
    {
        /// <summary>
        /// Adds the rules of the phase body to a request validator.
        /// </summary>
        /// <param name="richTextPolicy">Checks the description.</param>
        public void AddOperatingPhaseInputRules(IRichTextPolicy richTextPolicy)
        {
            validator.RuleFor(x => x.Code)
                .NotEmpty()
                .WithMessage(OperatingPhaseErrors.CodeRequired)
                .MaximumLength(OperatingPhaseValidationConstants.CodeMaxLength)
                .Must(MetricDefinitionRules.IsValidKey)
                .WithMessage(OperatingPhaseErrors.CodeInvalid)
                .SingleLineText();

            validator.RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(OperatingPhaseErrors.NameRequired)
                .MaximumLength(OperatingPhaseValidationConstants.NameMaxLength)
                .SingleLineText();

            validator.RuleFor(x => x.Description)
                .IsRichText(
                    richTextPolicy,
                    OperatingPhaseValidationConstants.DescriptionMaxLength,
                    OperatingPhaseErrors.DescriptionTooLong,
                    OperatingPhaseErrors.DescriptionNotAllowed,
                    OperatingPhaseErrors.DescriptionTooMuchFormatting);

            validator.RuleFor(x => x.Priority)
                .GreaterThanOrEqualTo(valueToCompare: 0)
                .WithMessage(OperatingPhaseErrors.PriorityNegative);

            validator.RuleFor(x => x.Conditions)
                .NotEmpty()
                .WithMessage(OperatingPhaseErrors.ConditionsRequired);

            validator.RuleForEach(x => x.Conditions).SetValidator(new PhaseConditionInputValidator());

            validator.RuleForEach(x => x.Limits).SetValidator(new PhaseLimitInputValidator());

            validator.RuleFor(x => x.Limits)
                .Must(limits => limits.Select(limit => limit.MetricDefinitionId).Distinct().Count() == limits.Count)
                .WithMessage(OperatingPhaseErrors.LimitMetricDuplicated);
        }
    }
}
