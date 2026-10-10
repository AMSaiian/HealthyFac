using System.Linq.Expressions;
using HealthyFac.Shared.Domain.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;

/// <summary>
/// Provides extension methods that add PostgreSQL check constraints to an entity, so the database mirrors
/// the application validation. Call them from the entity configuration, next to the property mapping.
/// The methods derive the column name with the same snake case rule as <c>ToSnakeCaseConventions</c>.
/// </summary>
public static class CheckConstraintBuilderExtensions
{
    /// <summary>
    /// Rejects a string that is empty or holds only whitespace.
    /// </summary>
    /// <param name="builder">The entity builder to extend.</param>
    /// <param name="property">The string property to check.</param>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>The <see cref="EntityTypeBuilder{TEntity}"/> instance for chained configuration.</returns>
    public static EntityTypeBuilder<TEntity> HasNotBlankCheck<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, string?>> property
    )
        where TEntity : class =>
        builder.AddCheck(property, CheckConstraintConstants.Names.NotBlank, CheckConstraintSql.NotBlank);

    /// <summary>
    /// Rejects a string whose length lies outside a closed range.
    /// </summary>
    /// <param name="builder">The entity builder to extend.</param>
    /// <param name="property">The string property to check.</param>
    /// <param name="minLength">The smallest allowed length.</param>
    /// <param name="maxLength">The largest allowed length.</param>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>The <see cref="EntityTypeBuilder{TEntity}"/> instance for chained configuration.</returns>
    public static EntityTypeBuilder<TEntity> HasLengthCheck<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, string?>> property,
        int minLength,
        int maxLength
    )
        where TEntity : class =>
        builder.AddCheck(
            property,
            CheckConstraintConstants.Names.Length,
            column => CheckConstraintSql.Length(column, minLength, maxLength));

    /// <summary>
    /// Rejects a numeric value outside a closed range.
    /// </summary>
    /// <param name="builder">The entity builder to extend.</param>
    /// <param name="property">The numeric property to check.</param>
    /// <param name="min">The smallest allowed value.</param>
    /// <param name="max">The largest allowed value.</param>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TValue">The numeric type of the property.</typeparam>
    /// <returns>The <see cref="EntityTypeBuilder{TEntity}"/> instance for chained configuration.</returns>
    public static EntityTypeBuilder<TEntity> HasRangeCheck<TEntity, TValue>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TValue>> property,
        TValue min,
        TValue max
    )
        where TEntity : class
        where TValue : struct, IFormattable =>
        builder.AddCheck(
            property,
            CheckConstraintConstants.Names.Range,
            column => CheckConstraintSql.Range(column, min, max));

    /// <summary>
    /// Rejects <see cref="Guid.Empty"/>.
    /// </summary>
    /// <param name="builder">The entity builder to extend.</param>
    /// <param name="property">The GUID property to check.</param>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>The <see cref="EntityTypeBuilder{TEntity}"/> instance for chained configuration.</returns>
    public static EntityTypeBuilder<TEntity> HasNotEmptyGuidCheck<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, Guid>> property
    )
        where TEntity : class =>
        builder.AddCheck(property, CheckConstraintConstants.Names.NotEmpty, CheckConstraintSql.NotEmptyGuid);

    /// <summary>
    /// Rejects any value that is not a declared member of the enumeration. Adding or renaming a member
    /// changes the constraint, so it needs a migration.
    /// </summary>
    /// <param name="builder">The entity builder to extend.</param>
    /// <param name="property">The enumeration property to check.</param>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <typeparam name="TEnum">The enumeration type of the property.</typeparam>
    /// <returns>The <see cref="EntityTypeBuilder{TEntity}"/> instance for chained configuration.</returns>
    public static EntityTypeBuilder<TEntity> HasEnumerationCheck<TEntity, TEnum>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TEnum?>> property
    )
        where TEntity : class
        where TEnum : Enumeration<TEnum> =>
        builder.AddCheck(
            property,
            CheckConstraintConstants.Names.Allowed,
            column => CheckConstraintSql.AllowedValues(
                column,
                Enumeration<TEnum>.All.Select(member => member.Value)));

    private static EntityTypeBuilder<TEntity> AddCheck<TEntity, TProperty>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, TProperty>> property,
        string rule,
        Func<string, string> buildSql
    )
        where TEntity : class
    {
        var column = property.GetMemberAccess().Name.ToSnakeCase();
        var name = CheckConstraintSql.ConstraintName(typeof(TEntity).Name.ToSnakeCase(), column, rule);

        builder.ToTable(table => table.HasCheckConstraint(name, buildSql(column)));

        return builder;
    }
}
