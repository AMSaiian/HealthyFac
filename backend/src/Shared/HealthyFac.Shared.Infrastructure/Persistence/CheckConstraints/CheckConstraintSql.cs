using System.Globalization;

namespace HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;

/// <summary>
/// Builds PostgreSQL check expressions and constraint names. Each expression mirrors one common
/// application validation rule, so the database rejects the same input that the validator rejects.
/// </summary>
public static class CheckConstraintSql
{
    /// <summary>
    /// Builds the expression for a value that holds at least one non-whitespace character.
    /// Mirrors the FluentValidation <c>NotEmpty</c> rule for a string.
    /// </summary>
    /// <param name="column">The database column name.</param>
    /// <returns>The check expression.</returns>
    public static string NotBlank(string column) =>
        string.Format(CultureInfo.InvariantCulture, CheckConstraintConstants.Templates.NotBlank, column);

    /// <summary>
    /// Builds the expression for a string whose length lies in a closed range.
    /// Counts code points, so a character outside the basic plane counts once.
    /// </summary>
    /// <param name="column">The database column name.</param>
    /// <param name="minLength">The smallest allowed length.</param>
    /// <param name="maxLength">The largest allowed length.</param>
    /// <returns>The check expression.</returns>
    public static string Length(string column, int minLength, int maxLength) =>
        string.Format(
            CultureInfo.InvariantCulture, CheckConstraintConstants.Templates.Length, column, minLength, maxLength);

    /// <summary>
    /// Builds the expression for a value in a closed range.
    /// </summary>
    /// <param name="column">The database column name.</param>
    /// <param name="min">The smallest allowed value.</param>
    /// <param name="max">The largest allowed value.</param>
    /// <typeparam name="TValue">A numeric type. The builder formats it with the invariant culture.</typeparam>
    /// <returns>The check expression.</returns>
    public static string Range<TValue>(string column, TValue min, TValue max)
        where TValue : struct, IFormattable =>
        string.Format(CultureInfo.InvariantCulture, CheckConstraintConstants.Templates.Range, column, min, max);

    /// <summary>
    /// Builds the expression for a GUID that is not <see cref="Guid.Empty"/>.
    /// </summary>
    /// <param name="column">The database column name.</param>
    /// <returns>The check expression.</returns>
    public static string NotEmptyGuid(string column) =>
        string.Format(
            CultureInfo.InvariantCulture, CheckConstraintConstants.Templates.NotEmptyGuid, column, Guid.Empty);

    /// <summary>
    /// Builds the expression for a value that belongs to a fixed set. Sorts the values, so the SQL stays
    /// stable between runs, and doubles every single quote inside a value.
    /// </summary>
    /// <param name="column">The database column name.</param>
    /// <param name="values">The allowed values.</param>
    /// <returns>The check expression.</returns>
    public static string AllowedValues(string column, IEnumerable<string> values) =>
        string.Format(
            CultureInfo.InvariantCulture,
            CheckConstraintConstants.Templates.Allowed,
            column,
            string.Join(
                CheckConstraintConstants.ValueSeparator,
                values
                    .Order(StringComparer.Ordinal)
                    .Select(value =>
                        CheckConstraintConstants.ValueQuote
                        + value.Replace(
                            CheckConstraintConstants.ValueQuote,
                            CheckConstraintConstants.ValueQuote + CheckConstraintConstants.ValueQuote)
                        + CheckConstraintConstants.ValueQuote)));

    /// <summary>
    /// Builds the constraint name from the entity, the column, and the rule.
    /// </summary>
    /// <param name="entity">The entity name in snake case.</param>
    /// <param name="column">The column name in snake case.</param>
    /// <param name="rule">The rule suffix from <see cref="CheckConstraintConstants.Names"/>.</param>
    /// <returns>The constraint name.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown by <see cref="CheckConstraintErrors.NameTooLong"/> when the name exceeds the PostgreSQL limit.
    /// </exception>
    public static string ConstraintName(string entity, string column, string rule)
    {
        var name = string.Join(
            CheckConstraintConstants.Names.Separator,
            CheckConstraintConstants.Names.Prefix,
            entity,
            column,
            rule);

        return name.Length > CheckConstraintConstants.MaxIdentifierLength
            ? throw CheckConstraintErrors.NameTooLong(name)
            : name;
    }
}
