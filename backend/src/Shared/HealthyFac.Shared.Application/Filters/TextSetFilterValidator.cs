using FluentValidation;
using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Shared.Application.Filters;

/// <summary>
/// Checks a set filter of strings: it holds its values, and each value is single-line text. Use it instead of
/// <see cref="SetFilterValidator{TValue}"/> for a <c>SetFilter&lt;string&gt;</c>, so the text rule covers each value.
/// </summary>
public sealed class TextSetFilterValidator : AbstractValidator<SetFilter<string>>
{
    public TextSetFilterValidator()
    {
        RuleFor(filter => filter.Values)
            .NotNull()
            .WithMessage(Grids.GridErrors.Validation.SetFilterValuesRequired);

        RuleForEach(filter => filter.Values).SingleLineText();
    }
}
