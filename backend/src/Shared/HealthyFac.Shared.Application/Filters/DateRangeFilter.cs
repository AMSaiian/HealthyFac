using System.Linq.Expressions;
using FluentValidation;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Shared.Application.Filters;

/// <summary>
/// Matches the rows whose instant lies in a half-open range. The caller converts the picked days to UTC instants.
/// </summary>
public sealed record DateRangeFilter
{
    /// <summary>The first instant of the range (inclusive), or <see langword="null"/> for an open start.</summary>
    public DateTime? From { get; init; }

    /// <summary>The first instant after the range (exclusive), or <see langword="null"/> for an open end.</summary>
    public DateTime? To { get; init; }

    /// <summary>
    /// Builds the predicate "the selected instant lies in the range". An open bound adds no condition.
    /// </summary>
    /// <param name="selector">Selects the instant column of the entity.</param>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The predicate for a <c>Where</c> call, or <see langword="null"/> when both bounds are open.</returns>
    public Expression<Func<T, bool>>? ToPredicate<T>(Expression<Func<T, DateTime>> selector) =>
        BuildPredicate<T>(Expression.Convert(selector.Body, typeof(DateTime?)), selector.Parameters[0]);

    /// <inheritdoc cref="ToPredicate{T}(Expression{Func{T, DateTime}})"/>
    public Expression<Func<T, bool>>? ToPredicate<T>(Expression<Func<T, DateTime?>> selector) =>
        BuildPredicate<T>(selector.Body, selector.Parameters[0]);

    private Expression<Func<T, bool>>? BuildPredicate<T>(Expression column, ParameterExpression parameter) =>
        Predicates
            .From(
                From is null
                    ? null
                    : ToLambda<T>(Expression.GreaterThanOrEqual(column, Bound(nameof(From))), parameter))
            .And(To is null ? null : ToLambda<T>(Expression.LessThan(column, Bound(nameof(To))), parameter));

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
/// Checks a date range filter. A bound must be a UTC instant: the validator rejects any other kind and never converts
/// it, so the query never meets a value that the database cannot compare with a <c>timestamp with time zone</c>.
/// </summary>
public sealed class DateRangeFilterValidator : AbstractValidator<DateRangeFilter>
{
    public DateRangeFilterValidator()
    {
        RuleFor(filter => filter.From!.Value)
            .Must(IsUtc)
            .When(filter => filter.From is not null)
            .WithMessage(GridErrors.Validation.DateRangeNotUtc);

        RuleFor(filter => filter.To!.Value)
            .Must(IsUtc)
            .When(filter => filter.To is not null)
            .WithMessage(GridErrors.Validation.DateRangeNotUtc);

        RuleFor(filter => filter)
            .Must(filter => filter.From < filter.To)
            .When(filter => filter is { From.Kind: DateTimeKind.Utc, To.Kind: DateTimeKind.Utc })
            .WithMessage(GridErrors.Validation.DateRangeFromNotBeforeTo);
    }

    private static bool IsUtc(DateTime value) => value.Kind == DateTimeKind.Utc;
}
