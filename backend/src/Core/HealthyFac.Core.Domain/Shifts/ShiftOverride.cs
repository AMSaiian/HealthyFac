using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Shifts;

/// <summary>
/// The work blocks of one employee on one local date. They replace the template for that date. An override without
/// blocks is a day off. A removal keeps the row (soft delete).
/// </summary>
public class ShiftOverride : IEntity, IAuditable, ISoftDeletable, IVersioned
{
    private readonly List<ShiftOverrideBlock> _blocks = [];

    /// <summary>
    /// Creates an override.
    /// </summary>
    /// <param name="employeeId">The employee of the override. It never changes.</param>
    /// <param name="date">The local date of the company. It never changes.</param>
    /// <param name="blocks">The blocks of the day. An empty list is a day off.</param>
    public ShiftOverride(Guid employeeId, DateOnly date, IEnumerable<BlockTimes> blocks)
        : this(employeeId, date)
    {
        AddBlocks(blocks);
    }

    private ShiftOverride(Guid employeeId, DateOnly date)
    {
        EmployeeId = employeeId;
        Date = date;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public uint RowVersion { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateOnly Date { get; private set; }
    public IReadOnlyCollection<ShiftOverrideBlock> Blocks => _blocks;

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Replaces all blocks. No block changes in place: the old blocks leave <see cref="Blocks"/> and new rows take
    /// their place.
    /// </summary>
    /// <remarks>The caller calls <c>Remove</c> on each returned block, so the soft delete stores the removal.</remarks>
    /// <param name="blocks">The new blocks. An empty list is a day off.</param>
    /// <returns>The old blocks.</returns>
    public IReadOnlyCollection<ShiftOverrideBlock> ReplaceBlocks(IEnumerable<BlockTimes> blocks)
    {
        var removedBlocks = _blocks.ToList();
        _blocks.Clear();
        AddBlocks(blocks);

        return removedBlocks;
    }

    private void AddBlocks(IEnumerable<BlockTimes> blocks) =>
        _blocks.AddRange(blocks.Select(times => new ShiftOverrideBlock(Id, times)));
}
