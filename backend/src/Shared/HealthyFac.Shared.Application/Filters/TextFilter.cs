using System.Linq.Expressions;
using System.Reflection;
using FluentValidation;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.TextInput;

namespace HealthyFac.Shared.Application.Filters;

/// <summary>
/// Matches the rows whose text starts with the value. The column collation decides the case rule.
/// </summary>
public sealed record TextFilter
{
    private static readonly MethodInfo StartsWithMethod =
        typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!;

    public required string Value { get; init; }

    /// <summary>
    /// Builds the predicate "the selected text starts with the value". The case-insensitive collation of the column
    /// ignores case (<c>backend-infrastructure.md</c>, <b>Text Comparison</b>). A LIKE character in the value
    /// matches itself, because the provider escapes it.
    /// </summary>
    /// <param name="selector">Selects the text column of the entity.</param>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The predicate for a <c>Where</c> call.</returns>
    public Expression<Func<T, bool>> ToPredicate<T>(Expression<Func<T, string>> selector)
    {
        var value = Expression.Property(Expression.Constant(this), nameof(Value));
        var body = Expression.Call(selector.Body, StartsWithMethod, value);

        return Expression.Lambda<Func<T, bool>>(body, selector.Parameters);
    }
}

/// <summary>
/// Checks that a text filter holds a value. JSON <c>null</c> reaches a <c>required</c> property, and the predicate
/// would throw on it.
/// </summary>
public sealed class TextFilterValidator : AbstractValidator<TextFilter>
{
    public TextFilterValidator()
    {
        RuleFor(filter => filter.Value)
            .NotNull()
            .WithMessage(GridErrors.Validation.TextFilterValueRequired)
            .SingleLineText();
    }
}
