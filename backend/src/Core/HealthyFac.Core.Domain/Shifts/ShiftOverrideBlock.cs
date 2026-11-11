using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Shifts;

/// <summary>
/// One work block of a <see cref="ShiftOverride"/>. A block never changes after it is created, and a removal keeps the
/// row (soft delete).
/// </summary>
public class ShiftOverrideBlock : IEntity, IAuditable, ISoftDeletable
{
    /// <summary>
    /// Creates a block.
    /// </summary>
    /// <param name="shiftOverrideId">The override of the block. It never changes.</param>
    /// <param name="times">The local start and end of the block.</param>
    internal ShiftOverrideBlock(Guid shiftOverrideId, BlockTimes times)
        : this(shiftOverrideId, times.Start, times.End)
    {
    }

    private ShiftOverrideBlock(Guid shiftOverrideId, TimeOnly start, TimeOnly end)
    {
        ShiftOverrideId = shiftOverrideId;
        Start = start;
        End = end;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid ShiftOverrideId { get; private set; }
    public TimeOnly Start { get; private set; }
    public TimeOnly End { get; private set; }

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
}
