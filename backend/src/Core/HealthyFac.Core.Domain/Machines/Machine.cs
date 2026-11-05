using System.Diagnostics.CodeAnalysis;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Machines;

/// <summary>
/// Represents a machine domain entity. A removal keeps the row (soft delete).
/// </summary>
public class Machine : IEntity, IAuditable, ISoftDeletable
{
    /// <summary>
    /// Creates a machine in the operational state.
    /// </summary>
    /// <param name="companyId">The company of the machine. It never changes.</param>
    /// <param name="departmentId">The department of the machine.</param>
    /// <param name="name">The name of the machine.</param>
    /// <param name="serialNumber">The serial number as the user typed it.</param>
    /// <param name="description">The description as HTML, or <see langword="null"/>.</param>
    public Machine(Guid companyId, Guid departmentId, string name, string serialNumber, string? description)
    {
        CompanyId = companyId;
        Update(departmentId, name, serialNumber, description);
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid CompanyId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public string Name { get; private set; }
    public string SerialNumber { get; private set; }

    /// <summary>The description as HTML. The Application layer checks it against the rich text policy.</summary>
    public string? Description { get; private set; }

    public MachineState State { get; private set; } = MachineState.Operational;

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Changes the data that a user edits. The company never changes, and <see cref="State"/> changes only through
    /// its own methods.
    /// </summary>
    /// <param name="departmentId">The department of the machine.</param>
    /// <param name="name">The name of the machine.</param>
    /// <param name="serialNumber">The serial number as the user typed it.</param>
    /// <param name="description">The description as HTML, or <see langword="null"/>.</param>
    [MemberNotNull(nameof(Name), nameof(SerialNumber))]
    public void Update(Guid departmentId, string name, string serialNumber, string? description)
    {
        DepartmentId = departmentId;
        Name = name;
        SerialNumber = serialNumber;
        Description = description;
    }
}
