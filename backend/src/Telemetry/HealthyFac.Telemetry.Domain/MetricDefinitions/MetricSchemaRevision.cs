using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Telemetry.Domain.MetricDefinitions;

/// <summary>
/// A numbered, stored payload schema of one machine. Each apply of metric changes adds one, and the intake reads the
/// last one. The row is append-only: it has no audit fields and no soft delete.
/// </summary>
public class MetricSchemaRevision : IEntity
{
    /// <summary>
    /// Creates a schema revision.
    /// </summary>
    /// <param name="machineRegistrationId">The registration of the machine.</param>
    /// <param name="revision">The number of the revision. It starts at 0 and has no gap.</param>
    /// <param name="schema">The JSON Schema as text.</param>
    /// <param name="createdAt">The moment of the revision, as a UTC instant.</param>
    public MetricSchemaRevision(Guid machineRegistrationId, int revision, string schema, DateTime createdAt)
    {
        MachineRegistrationId = machineRegistrationId;
        Revision = revision;
        Schema = schema;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid MachineRegistrationId { get; private set; }
    public int Revision { get; private set; }
    public string Schema { get; private set; }
    public DateTime CreatedAt { get; private set; }
}
