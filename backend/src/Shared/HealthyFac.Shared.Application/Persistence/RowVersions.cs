namespace HealthyFac.Shared.Application.Persistence;

/// <summary>
/// Defines the name of the row version property of an entity that uses optimistic concurrency.
/// </summary>
public static class RowVersions
{
    /// <summary>
    /// The PostgreSQL system column that changes on every row update. A <see langword="uint"/> shadow property with
    /// this name maps to it, so two writers that save the same row cannot both succeed. The name is already snake case,
    /// so the naming convention keeps it. A use case reads it with <c>EF.Property</c> and sets its original value from
    /// the value that the client sent.
    /// </summary>
    public const string PropertyName = "xmin";
}
