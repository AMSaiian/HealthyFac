using System.Linq.Expressions;
using FluentValidation;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Shared.Application.Filters;

/// <summary>
/// Matches the rows whose day lies in a half-open range of days. The days carry no time zone.
/// </summary>
public sealed record DateOnlyRangeFilter
{
    /// <summary>The first day of the range (inclusive), or <see langword="null"/> for an open start.</summary>
    public DateOnly? From { get; init; }

    /// <summary>The first day after the range (exclusive), or <see langword="null"/> for an open end.</summary>
    public DateOnly? To { get; init; }

    /// <summary>
    /// Builds the predicate "the selected day lies in the range". An open bound adds no condition.
    /// </summary>
    /// <param name="selector">Selects the day column of the entity.</param>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The predicate for a <c>Where</c> call, or <see langword="null"/> when both bounds are open.</returns>
    public Expression<Func<T, bool>>? ToPredicate<T>(Expression<Func<T, DateOnly>> selector) =>
        Predicates
            .From(
                From is null
                    ? null
                    : ToLambda<T>(
                        Expression.GreaterThanOrEqual(
                            Expression.Convert(selector.Body, typeof(DateOnly?)),
                            Bound(nameof(From))),
                        selector.Parameters[index: 0]))
            .And(
                To is null
                    ? null
                    : ToLambda<T>(
                        Expression.LessThan(
                            Expression.Convert(selector.Body, typeof(DateOnly?)),
                            Bound(nameof(To))),
                        selector.Parameters[index: 0]));

    private static Expression<Func<T, bool>> ToLambda<T>(Expression body, ParameterExpression parameter) =>
        Expression.Lambda<Func<T, bool>>(body, parameter);

    /// <summary>
    /// Reads a bound through a member of this record, so EF Core sends it as a parameter.
    /// </summary>
    /// <param name="name">The name of the bound property.</param>
    /// <returns>The member access expression.</returns>
    private MemberExpression Bound(string name) => Expression.Property(Expression.Constant(this), name);
}

/// <summary>
/// Checks a day range filter: the first day must be before the first day after the range.
/// </summary>
public sealed class DateOnlyRangeFilterValidator : AbstractValidator<DateOnlyRangeFilter>
{
    public DateOnlyRangeFilterValidator()
    {
        RuleFor(filter => filter)
            .Must(filter => filter.From < filter.To)
            .When(filter => filter is { From: not null, To: not null })
            .WithMessage(GridErrors.Validation.DateRangeFromNotBeforeTo);
    }
}
