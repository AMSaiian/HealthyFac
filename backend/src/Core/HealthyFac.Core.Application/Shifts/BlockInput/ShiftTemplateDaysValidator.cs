using FluentValidation;
using HealthyFac.Core.Domain.Shifts;

namespace HealthyFac.Core.Application.Shifts;

/// <summary>
/// Checks the days of a template version: exactly one entry for each weekday, and valid blocks on each day.
/// </summary>
public sealed class ShiftTemplateDaysValidator : AbstractValidator<IReadOnlyList<ShiftTemplateDayInput>>
{
    public ShiftTemplateDaysValidator()
    {
        RuleFor(days => days)
            .Must(days => days.Count == ShiftValidationConstants.WeekdayCount
                && days.Select(day => day.Weekday).Distinct().Count() == Weekday.All.Count)
            .WithMessage(ShiftErrors.Validation.DaysInvalid);

        RuleForEach(days => days)
            .ChildRules(day => day.RuleFor(entry => entry.Weekday)
                .NotNull()
                .WithMessage(ShiftErrors.Validation.WeekdayRequired));

        RuleForEach(days => days)
            .ChildRules(day => day.RuleFor(entry => entry.Blocks)
                .NotNull()
                .WithMessage(ShiftErrors.Validation.BlocksRequired)
                .SetValidator(new BlockListValidator())
                .When(entry => entry.Blocks is not null));
    }
}
