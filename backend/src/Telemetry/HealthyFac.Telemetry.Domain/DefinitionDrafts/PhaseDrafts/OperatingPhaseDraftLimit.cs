using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Telemetry.Domain.DefinitionDrafts.PhaseDrafts;

/// <summary>
/// A proposed limit row of an <see cref="OperatingPhaseDraft"/>. It names its metric as a definition or as a draft of
/// a definition. A removal keeps the row (soft delete).
/// </summary>
public sealed class OperatingPhaseDraftLimit : IEntity, IAuditable, ISoftDeletable
{
    private OperatingPhaseDraftLimit()
    {
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid? MetricDefinitionId { get; private set; }
    public Guid? MetricDraftId { get; private set; }
    public double? WarningLow { get; private set; }
    public double? WarningHigh { get; private set; }
    public double? AlarmLow { get; private set; }
    public double? AlarmHigh { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Tells whether the row still waits for a draft of a definition.
    /// </summary>
    public bool IsWaitingForDraft => MetricDraftId is not null;

    /// <summary>
    /// Creates a limit row from the proposed values.
    /// </summary>
    /// <param name="values">The proposed values.</param>
    /// <returns>The new row.</returns>
    public static OperatingPhaseDraftLimit Create(PhaseDraftLimitValues values) =>
        new()
        {
            MetricDefinitionId = values.MetricDefinitionId,
            MetricDraftId = values.MetricDraftId,
            WarningLow = values.WarningLow,
            WarningHigh = values.WarningHigh,
            AlarmLow = values.AlarmLow,
            AlarmHigh = values.AlarmHigh,
        };

    /// <summary>
    /// Moves the metric from a draft to the definition that the draft became.
    /// </summary>
    /// <param name="draftId">The applied metric draft.</param>
    /// <param name="definitionId">The new metric definition.</param>
    /// <returns><see langword="true"/> when the row used the draft.</returns>
    public bool Retarget(Guid draftId, Guid definitionId)
    {
        if (MetricDraftId != draftId)
        {
            return false;
        }

        MetricDraftId = null;
        MetricDefinitionId = definitionId;

        return true;
    }
}
