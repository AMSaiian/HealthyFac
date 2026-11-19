using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Telemetry.Domain.DefinitionDrafts.PhaseDrafts;

/// <summary>
/// A proposed condition of an <see cref="OperatingPhaseDraft"/>. It names its source as a definition or as a draft of
/// a definition. When a user applies the draft of a definition, the retarget methods move the condition to the new
/// definition. A removal keeps the row (soft delete).
/// </summary>
public sealed class OperatingPhaseDraftCondition : IEntity, IAuditable, ISoftDeletable
{
    private List<string>? _states;

    private OperatingPhaseDraftCondition()
    {
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid? MetricDefinitionId { get; private set; }
    public Guid? MetricDraftId { get; private set; }
    public Guid? StatusDefinitionId { get; private set; }
    public Guid? StatusDraftId { get; private set; }

    /// <summary>One of the phase condition operators, or <see langword="null"/>.</summary>
    public string? Operator { get; private set; }

    public double? Value { get; private set; }
    public double? UpperValue { get; private set; }
    public IReadOnlyList<string>? States => _states;
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    /// <summary>
    /// Tells whether the condition still waits for a draft of a definition.
    /// </summary>
    public bool IsWaitingForDraft => MetricDraftId is not null || StatusDraftId is not null;

    /// <summary>
    /// Creates a condition from the proposed values.
    /// </summary>
    /// <param name="values">The proposed values.</param>
    /// <returns>The new condition.</returns>
    public static OperatingPhaseDraftCondition Create(PhaseDraftConditionValues values) =>
        new()
        {
            MetricDefinitionId = values.MetricDefinitionId,
            MetricDraftId = values.MetricDraftId,
            StatusDefinitionId = values.StatusDefinitionId,
            StatusDraftId = values.StatusDraftId,
            Operator = values.Operator,
            Value = values.Value,
            UpperValue = values.UpperValue,
            _states = values.States is null ? null : [.. values.States],
        };

    /// <summary>
    /// Moves a metric source from a draft to the definition that the draft became.
    /// </summary>
    /// <param name="draftId">The applied metric draft.</param>
    /// <param name="definitionId">The new metric definition.</param>
    /// <returns><see langword="true"/> when the condition used the draft.</returns>
    public bool RetargetMetric(Guid draftId, Guid definitionId)
    {
        if (MetricDraftId != draftId)
        {
            return false;
        }

        MetricDraftId = null;
        MetricDefinitionId = definitionId;

        return true;
    }

    /// <summary>
    /// Moves a status source from a draft to the definition that the draft became.
    /// </summary>
    /// <param name="draftId">The applied status draft.</param>
    /// <param name="definitionId">The new status definition.</param>
    /// <returns><see langword="true"/> when the condition used the draft.</returns>
    public bool RetargetStatus(Guid draftId, Guid definitionId)
    {
        if (StatusDraftId != draftId)
        {
            return false;
        }

        StatusDraftId = null;
        StatusDefinitionId = definitionId;

        return true;
    }
}
