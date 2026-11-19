using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthyFac.Shared.Infrastructure.Persistence.SoftDelete.Dependents;

/// <summary>The input of the soft delete of the untracked rows of one cascade relation.</summary>
internal sealed record DependentDeletionContext
{
    /// <summary>The context that is about to save. It needs an open transaction.</summary>
    public required DbContext DbContext { get; init; }

    /// <summary>The cascade relation, with the dependent type as its declaring type.</summary>
    public required IForeignKey Relation { get; init; }

    /// <summary>The keys of the deleted principals.</summary>
    public required IReadOnlyCollection<Guid> PrincipalKeys { get; init; }

    /// <summary>The keys of the dependents that the context tracks. The save handles them.</summary>
    public required IReadOnlyCollection<Guid> TrackedKeys { get; init; }

    /// <summary>Whether the dependent type is the principal of a cascade relation.</summary>
    public required bool HasOwnCascade { get; init; }

    /// <summary>The delete time.</summary>
    public required DateTime DeletedAt { get; init; }

    /// <summary>The actor.</summary>
    public required Guid? DeletedBy { get; init; }
}
