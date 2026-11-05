using System.Linq.Expressions;
using System.Reflection;
using FluentValidation;
using HealthyFac.Shared.Application.Grids;

namespace HealthyFac.Shared.Application.Filters;

/// <summary>
/// Matches the rows whose value is one of the given values (SQL <c>IN</c>).
/// </summary>
/// <typeparam name="TValue">The type of the column value.</typeparam>
public sealed record SetFilter<TValue>
{
    private static readonly MethodInfo ContainsMethod = typeof(Enumerable)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(method => method.Name == nameof(Enumerable.Contains) && method.GetParameters().Length == 2)
        .MakeGenericMethod(typeof(TValue));

    public required IReadOnlyList<TValue> Values { get; init; }

    /// <summary>
    /// Builds the predicate "the selected value is in the set".
    /// </summary>
    /// <param name="selector">Selects the column of the entity.</param>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The predicate for a <c>Where</c> call.</returns>
    public Expression<Func<T, bool>> ToPredicate<T>(Expression<Func<T, TValue>> selector)
    {
        var values = Expression.Property(Expression.Constant(this), nameof(Values));
        var body = Expression.Call(ContainsMethod, values, selector.Body);

        return Expression.Lambda<Func<T, bool>>(body, selector.Parameters);
    }
}

/// <summary>
/// Checks that a set filter holds its values. JSON <c>null</c> reaches a <c>required</c> property, and the predicate
/// would throw on it.
/// </summary>
/// <typeparam name="TValue">The type of the column value.</typeparam>
public sealed class SetFilterValidator<TValue> : AbstractValidator<SetFilter<TValue>>
{
    public SetFilterValidator()
    {
        RuleFor(filter => filter.Values)
            .NotNull()
            .WithMessage(GridErrors.Validation.SetFilterValuesRequired);
    }
}
