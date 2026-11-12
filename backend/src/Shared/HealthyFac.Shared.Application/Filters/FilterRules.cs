using FluentValidation;

namespace HealthyFac.Shared.Application.Filters;

/// <summary>
/// Adds the rules of one filter field to a validator. Call it inside a <c>When(filter => filter.Field is not null, ...)</c>
/// block, on <c>RuleFor(filter => filter.Field!)</c>: every filter field is optional, and the validator states it before
/// the shared rule. The <c>!</c> keeps the rule builder non-nullable, as the validators require.
/// </summary>
public static class FilterRules
{
    extension<T>(IRuleBuilderInitial<T, TextFilter> rule)
    {
        /// <summary>
        /// Checks the text filter and limits the length of its value.
        /// </summary>
        /// <param name="maxLength">The longest value that the filter accepts, in characters.</param>
        /// <returns>The rule builder, for further rules on the same field.</returns>
        public IRuleBuilderOptions<T, TextFilter> IsTextFilter(int maxLength) =>
            rule.SetValidator(new TextFilterValidator())
                .ChildRules(child => child.RuleFor(filter => filter.Value).MaximumLength(maxLength));
    }

    extension<T>(IRuleBuilderInitial<T, DateRangeFilter> rule)
    {
        /// <summary>
        /// Checks the date range filter with its validator.
        /// </summary>
        /// <returns>The rule builder, for further rules on the same field.</returns>
        public IRuleBuilderOptions<T, DateRangeFilter> IsDateRangeFilter() =>
            rule.SetValidator(new DateRangeFilterValidator());
    }

    extension<T, TValue>(IRuleBuilderInitial<T, SetFilter<TValue>> rule)
    {
        /// <summary>
        /// Checks the set filter with its validator.
        /// </summary>
        /// <returns>The rule builder, for further rules on the same field.</returns>
        public IRuleBuilderOptions<T, SetFilter<TValue>> IsSetFilter() =>
            rule.SetValidator(new SetFilterValidator<TValue>());
    }
}
