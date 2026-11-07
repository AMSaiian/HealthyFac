using System.Diagnostics.CodeAnalysis;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Telemetry.Domain.StatusDefinitions;

/// <summary>
/// Represents a status that a machine can report, with its severity. A user defines it for a machine. A removal keeps
/// the row (soft delete).
/// </summary>
public class StatusDefinition : IEntity, IAuditable, ISoftDeletable
{
    /// <summary>
    /// Creates a status definition of a machine.
    /// </summary>
    /// <param name="machineRegistrationId">The registration of the machine. It never changes.</param>
    /// <param name="code">The code as the user typed it.</param>
    /// <param name="severity">How serious the status is.</param>
    /// <param name="description">The description as HTML, or <see langword="null"/>.</param>
    public StatusDefinition(Guid machineRegistrationId, string code, StatusSeverity severity, string? description)
    {
        MachineRegistrationId = machineRegistrationId;
        Update(code, severity, description);
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid MachineRegistrationId { get; private set; }
    public string Code { get; private set; }
    public StatusSeverity Severity { get; private set; }

    /// <summary>The description as HTML. The Application layer checks it against the rich text policy.</summary>
    public string? Description { get; private set; }

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Changes the data that a user edits. The machine never changes.
    /// </summary>
    /// <param name="code">The code as the user typed it.</param>
    /// <param name="severity">How serious the status is.</param>
    /// <param name="description">The description as HTML, or <see langword="null"/>.</param>
    [MemberNotNull(nameof(Code), nameof(Severity))]
    public void Update(string code, StatusSeverity severity, string? description)
    {
        Code = code;
        Severity = severity;
        Description = description;
    }
}
