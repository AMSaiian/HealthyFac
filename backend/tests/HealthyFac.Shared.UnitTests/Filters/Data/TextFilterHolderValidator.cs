using FluentValidation;
using HealthyFac.Shared.Application.Filters;

namespace HealthyFac.Shared.UnitTests.Filters.Data;

public sealed class TextFilterHolderValidator : AbstractValidator<TextFilterHolder>
{
    public TextFilterHolderValidator()
    {
        RuleFor(holder => holder.Name!).IsTextFilter(FilterRulesTestData.MaxLength);
    }
}
