namespace HealthyFac.Shared.Domain.Entities;

/// <summary>
/// Marks an entity that two writers must not both save. The persistence layer maps
/// <see cref="RowVersion"/> to the PostgreSQL system column <c>xmin</c> as a concurrency token, so a save of an
/// old version fails. Only the database writes the value.
/// </summary>
public interface IVersioned
{
    /// <summary>The version of the stored row. It changes on every update of the row.</summary>
    uint RowVersion { get; }
}
