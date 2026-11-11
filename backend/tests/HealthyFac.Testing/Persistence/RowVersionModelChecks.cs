using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Testing.Persistence;

/// <summary>
/// Checks that every row version of a model is the <see cref="IVersioned.RowVersion"/> property, mapped by
/// <c>ConfigureVersioned()</c>. A versioned entity without that mapping saves without a concurrency check and
/// shows no error, so only a model check finds it.
/// </summary>
public static class RowVersionModelChecks
{
    /// <summary>
    /// Finds the wrong row versions of the entity types.
    /// </summary>
    /// <param name="entityTypes">The entity types of the design-time model.</param>
    /// <returns>One line per wrong property: a concurrency token outside <see cref="IVersioned"/>, or a
    /// <see cref="IVersioned.RowVersion"/> that is not a row version on <c>xmin</c>.</returns>
    public static IReadOnlyList<string> FindWrongRowVersions(IEnumerable<IEntityType> entityTypes) =>
        [.. entityTypes.SelectMany(FindWrongRowVersions)];

    private static IEnumerable<string> FindWrongRowVersions(IEntityType entityType)
    {
        var isVersioned = typeof(IVersioned).IsAssignableFrom(entityType.ClrType);
        var foreignTokens = entityType.GetDeclaredProperties()
            .Where(property => property.IsConcurrencyToken)
            .Where(property => !isVersioned || property.Name != nameof(IVersioned.RowVersion))
            .Select(property => $"{entityType.DisplayName()}.{property.Name}: a concurrency token outside IVersioned");

        if (!isVersioned)
        {
            return foreignTokens;
        }

        var rowVersion = entityType.FindProperty(nameof(IVersioned.RowVersion));
        var isMapped = rowVersion is { IsConcurrencyToken: true, ValueGenerated: ValueGenerated.OnAddOrUpdate }
            && rowVersion.GetColumnName() == PersistenceConstants.RowVersionColumn;

        return isMapped
            ? foreignTokens
            : foreignTokens.Append($"{entityType.DisplayName()}.RowVersion: not mapped with ConfigureVersioned()");
    }
}
