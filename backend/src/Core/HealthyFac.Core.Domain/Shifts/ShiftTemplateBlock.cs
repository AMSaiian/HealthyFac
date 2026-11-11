using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Core.Domain.Shifts;

/// <summary>
/// One work block of a weekday in a <see cref="ShiftTemplateVersion"/>. A block never changes after it is created, and
/// a removal keeps the row (soft delete).
/// </summary>
public class ShiftTemplateBlock : IEntity, IAuditable, ISoftDeletable
{
    /// <summary>
    /// Creates a block.
    /// </summary>
    /// <param name="shiftTemplateVersionId">The version of the block. It never changes.</param>
    /// <param name="weekday">The weekday of the block.</param>
    /// <param name="times">The local start and end of the block.</param>
    internal ShiftTemplateBlock(Guid shiftTemplateVersionId, Weekday weekday, BlockTimes times)
        : this(shiftTemplateVersionId, weekday, times.Start, times.End)
    {
    }

    private ShiftTemplateBlock(Guid shiftTemplateVersionId, Weekday weekday, TimeOnly start, TimeOnly end)
    {
        ShiftTemplateVersionId = shiftTemplateVersionId;
        Weekday = weekday;
        Start = start;
        End = end;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid ShiftTemplateVersionId { get; private set; }
    public Weekday Weekday { get; private set; }
    public TimeOnly Start { get; private set; }
    public TimeOnly End { get; private set; }

    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
}
