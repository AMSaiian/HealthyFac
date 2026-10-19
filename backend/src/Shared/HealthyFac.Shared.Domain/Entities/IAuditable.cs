namespace HealthyFac.Shared.Domain.Entities;

/// <summary>
/// Marks an entity that records who created and changed it, and when. The persistence layer sets these values
/// when it saves, so an entity never sets them itself. All times are UTC.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; }

    /// <summary>The time of the last change, or <see langword="null"/> when the entity never changed.</summary>
    DateTime? UpdatedAt { get; }

    Guid CreatedBy { get; }

    Guid? UpdatedBy { get; }
}
