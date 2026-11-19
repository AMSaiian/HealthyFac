using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Shared.Infrastructure.Persistence.SoftDelete.Dependents;

/// <summary>The input of the soft delete of the untracked rows of one cascade relation.</summary>
internal sealed record DependentDeletionContext
{
    /// <summary>The cascade relation, with the dependent type as its declaring type.</summary>
    public required IForeignKey Relation { get; init; }

    public required IReadOnlyCollection<Guid> PrincipalKeys { get; init; }

    /// <summary>The keys of the dependents that the context tracks. The save handles them.</summary>
    public required IReadOnlyCollection<Guid> TrackedKeys { get; init; }

    /// <summary>Whether the dependent type is the principal of a cascade relation.</summary>
    public required bool HasOwnCascade { get; init; }

    public required DateTime DeletedAt { get; init; }

    public required Guid? DeletedBy { get; init; }
}
