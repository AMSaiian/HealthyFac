using System.Diagnostics.CodeAnalysis;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Machines;

/// <summary>
/// Represents a machine domain entity. A removal keeps the row (soft delete).
/// </summary>
public class Machine : IEntity, IAuditable, ISoftDeletable
{
    private readonly List<MachineStateHistory> _stateHistory = [];

    /// <summary>
    /// Creates a machine in the operational state.
    /// </summary>
    /// <param name="companyId">The company of the machine. It never changes.</param>
    /// <param name="departmentId">The department of the machine.</param>
    /// <param name="name">The name of the machine.</param>
    /// <param name="serialNumber">The serial number as the user typed it.</param>
    /// <param name="description">The description as HTML, or <see langword="null"/>.</param>
    /// <param name="startedAt">The moment the machine starts in the operational state, as a UTC instant.</param>
    public Machine(
        Guid companyId,
        Guid departmentId,
        string name,
        string serialNumber,
        string? description,
        DateTime startedAt
    )
        : this(companyId, departmentId, name, serialNumber, description)
    {
        _stateHistory.Add(new MachineStateHistory(Id, MachineState.Operational, startedAt));
    }

    private Machine(Guid companyId, Guid departmentId, string name, string serialNumber, string? description)
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

    public IReadOnlyCollection<MachineStateHistory> StateHistory => _stateHistory;

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Changes the data that a user edits. The company never changes, and the state changes only through
    /// <see cref="ChangeState"/>.
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

    /// <summary>
    /// Starts a new state of the machine.
    /// </summary>
    /// <remarks>
    /// The guards read the loaded <see cref="StateHistory"/>. The caller must load the full history first, because a
    /// partial history can pass the guard with a <paramref name="startedAt"/> that is not the latest.
    /// </remarks>
    /// <param name="state">The new state.</param>
    /// <param name="startedAt">The moment the state starts, as a UTC instant.</param>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="startedAt"/> is not later than the start of the latest row, or <paramref name="state"/> is the
    /// current state.
    /// </exception>
    public void ChangeState(MachineState state, DateTime startedAt)
    {
        var latest = _stateHistory.MaxBy(row => row.StartedAt);

        if (latest is null)
        {
            _stateHistory.Add(new MachineStateHistory(Id, state, startedAt));

            return;
        }

        if (startedAt <= latest.StartedAt)
        {
            throw MachineStateErrors.StartNotAfterLatest(Id, startedAt, latest.StartedAt);
        }

        if (state == latest.State)
        {
            throw MachineStateErrors.SameState(Id, state);
        }

        _stateHistory.Add(new MachineStateHistory(Id, state, startedAt));
    }
}
