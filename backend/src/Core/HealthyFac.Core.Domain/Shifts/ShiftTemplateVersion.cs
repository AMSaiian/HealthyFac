using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Shifts;

/// <summary>
/// The weekly work blocks of one employee from <see cref="EffectiveFrom"/> on, until a later version takes over. A
/// removal keeps the row (soft delete).
/// </summary>
public class ShiftTemplateVersion : IEntity, IAuditable, ISoftDeletable, IVersioned
{
    private readonly List<ShiftTemplateBlock> _blocks = [];

    /// <summary>
    /// Creates a version.
    /// </summary>
    /// <param name="employeeId">The employee of the version. It never changes.</param>
    /// <param name="effectiveFrom">The first local date of the company on which the version applies.</param>
    /// <param name="blocks">The blocks of the version.</param>
    public ShiftTemplateVersion(
        Guid employeeId,
        DateOnly effectiveFrom,
        IEnumerable<(Weekday Weekday, BlockTimes Times)> blocks
    )
        : this(employeeId, effectiveFrom)
    {
        AddBlocks(blocks);
    }

    private ShiftTemplateVersion(Guid employeeId, DateOnly effectiveFrom)
    {
        EmployeeId = employeeId;
        EffectiveFrom = effectiveFrom;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public uint RowVersion { get; private set; }
    public Guid EmployeeId { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public IReadOnlyCollection<ShiftTemplateBlock> Blocks => _blocks;

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
    /// <param name="blocks">The new blocks.</param>
    /// <returns>The old blocks.</returns>
    public IReadOnlyCollection<ShiftTemplateBlock> ReplaceBlocks(
        IEnumerable<(Weekday Weekday, BlockTimes Times)> blocks
    )
    {
        var removedBlocks = _blocks.ToList();
        _blocks.Clear();
        AddBlocks(blocks);

        return removedBlocks;
    }

    private void AddBlocks(IEnumerable<(Weekday Weekday, BlockTimes Times)> blocks) =>
        _blocks.AddRange(blocks.Select(block => new ShiftTemplateBlock(Id, block.Weekday, block.Times)));
}
