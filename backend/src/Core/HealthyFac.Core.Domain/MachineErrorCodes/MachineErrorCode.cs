using System.Diagnostics.CodeAnalysis;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.MachineErrorCodes;

/// <summary>
/// Represents an error code that a user defines for a machine. A removal keeps the row (soft delete).
/// </summary>
public class MachineErrorCode : IEntity, IAuditable, ISoftDeletable
{
    /// <summary>
    /// Creates an error code of a machine.
    /// </summary>
    /// <param name="machineId">The machine of the error code. It never changes.</param>
    /// <param name="code">The code as the user typed it.</param>
    /// <param name="description">The description as HTML, or <see langword="null"/>.</param>
    public MachineErrorCode(Guid machineId, string code, string? description)
    {
        MachineId = machineId;
        Update(code, description);
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid MachineId { get; private set; }
    public string Code { get; private set; }

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
    /// <param name="description">The description as HTML, or <see langword="null"/>.</param>
    [MemberNotNull(nameof(Code))]
    public void Update(string code, string? description)
    {
        Code = code;
        Description = description;
    }
}
