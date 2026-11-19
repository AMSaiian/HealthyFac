using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Testing.Persistence;

/// <summary>
/// Checks that every relation of a model follows the aggregate rule: a collection of a root is a child entity with
/// a required foreign key and <see cref="DeleteBehavior.Cascade"/>, and every other relation is
/// <see cref="DeleteBehavior.Restrict"/>. A wrong behavior shows no error until a delete, so only a model check
/// finds it.
/// </summary>
public static class AggregateRelationModelChecks
{
    /// <summary>
    /// Finds the wrong relations of the entity types. A one-to-one ownership (<c>OwnsOne</c>) is skipped, because
    /// the owned values live in the table of the owner.
    /// </summary>
    /// <param name="entityTypes">The entity types of the design-time model.</param>
    /// <returns>One line per wrong relation: an <c>OwnsMany</c>, or a foreign key whose delete behavior is not the
    /// expected one.</returns>
    public static IReadOnlyList<string> FindWrongRelations(IEnumerable<IEntityType> entityTypes) =>
    [
        .. entityTypes
            .SelectMany(entityType => entityType.GetDeclaredForeignKeys())
            .Select(Describe)
            .OfType<string>()
            .Order(StringComparer.Ordinal),
    ];

    private static string? Describe(IForeignKey foreignKey)
    {
        if (foreignKey.IsOwnership)
        {
            return foreignKey.IsUnique
                ? null
                : $"{foreignKey.PrincipalEntityType.DisplayName()}.{foreignKey.PrincipalToDependent?.Name}: OwnsMany";
        }

        var expected = foreignKey is { PrincipalToDependent.IsCollection: true, IsRequired: true }
            ? DeleteBehavior.Cascade
            : DeleteBehavior.Restrict;
        if (foreignKey.DeleteBehavior == expected)
        {
            return null;
        }

        var properties = string.Join(separator: ", ", foreignKey.Properties.Select(property => property.Name));

        return $"{foreignKey.DeclaringEntityType.DisplayName()}.{properties} -> "
            + $"{foreignKey.PrincipalEntityType.DisplayName()}: {foreignKey.DeleteBehavior}, expected {expected}";
    }
}
