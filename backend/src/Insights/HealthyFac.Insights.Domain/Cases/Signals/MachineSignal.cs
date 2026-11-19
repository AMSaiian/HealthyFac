using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Insights.Domain.Cases.Signals;

/// <summary>
/// The copy of one Telemetry signal episode in Insights, with the coverage that the maintenance work gives it. The ID
/// is the ID of the episode, so a repeated message finds its row. <see cref="Sequence"/> is the sequence of the last
/// message that the row applied: an older or repeated message changes nothing.
/// </summary>
public class MachineSignal : IEntity, IVersioned
{
    /// <summary>
    /// Creates the signal from its first message. The signal starts <c>new</c>, without a case.
    /// </summary>
    /// <param name="episodeId">The ID of the Telemetry episode.</param>
    /// <param name="machineId">The machine of the episode.</param>
    /// <param name="companyId">The company of the machine.</param>
    /// <param name="change">The values of the first message.</param>
    public MachineSignal(Guid episodeId, Guid machineId, Guid companyId, SignalChange change)
    {
        Id = episodeId;
        MachineId = machineId;
        CompanyId = companyId;
        Coverage = SignalCoverage.New;
        Rule = change.Rule;
        Severity = change.Severity;
        State = change.State;
        OpenedAt = change.OpenedAt;
        Copy(change);
    }

    private MachineSignal()
    {
        Rule = null!;
        Severity = null!;
        State = null!;
        Coverage = null!;
    }

    public Guid Id { get; private set; }

    public uint RowVersion { get; private set; }

    public Guid MachineId { get; private set; }

    public Guid CompanyId { get; private set; }

    public SignalRule Rule { get; private set; }

    public SignalSeverity Severity { get; private set; }

    public SignalState State { get; private set; }

    public Guid? MetricDefinitionId { get; private set; }

    public string? MetricKey { get; private set; }

    public Guid? StatusDefinitionId { get; private set; }

    public string? StatusCode { get; private set; }

    public string? PhaseCode { get; private set; }

    public double? Value { get; private set; }

    public double? Limit { get; private set; }

    public double? Z { get; private set; }

    public double? ForecastSeconds { get; private set; }

    public DateTime OpenedAt { get; private set; }

    public DateTime? ResolvedAt { get; private set; }

    public int Sequence { get; private set; }

    public Guid? CaseId { get; private set; }

    public SignalCoverage Coverage { get; private set; }

    public Guid? WorkOrderDraftId { get; private set; }

    public Guid? WorkOrderId { get; private set; }

    /// <summary>Tells whether the episode is still open in Telemetry.</summary>
    public bool IsOpen => State == SignalState.Open;

    /// <summary>
    /// Applies a later message of the episode.
    /// </summary>
    /// <param name="change">The values of the message.</param>
    /// <returns>
    /// <see langword="false"/> when the sequence of the message is not above the stored one, so nothing changed.
    /// </returns>
    public bool Apply(SignalChange change)
    {
        if (change.Sequence <= Sequence)
        {
            return false;
        }

        var crossingValue = Value;
        var crossingLimit = Limit;
        Severity = change.Severity;
        State = change.State;
        Copy(change);

        if (change.State == SignalState.Resolved)
        {
            Value = crossingValue;
            Limit = crossingLimit;
        }

        return true;
    }

    /// <summary>
    /// Puts the signal into a case.
    /// </summary>
    /// <param name="caseId">The case.</param>
    public void JoinCase(Guid caseId) => CaseId = caseId;

    /// <summary>
    /// Marks the signal as new again, for an escalation of a dismissed signal.
    /// </summary>
    public void Reopen() => Coverage = SignalCoverage.New;

    /// <summary>
    /// Marks the signal as watched: the triage looks at it later.
    /// </summary>
    public void Watch() => Coverage = SignalCoverage.Watched;

    /// <summary>
    /// Marks the signal as dismissed: the triage or a user found no need for work.
    /// </summary>
    public void Dismiss() => Coverage = SignalCoverage.Dismissed;

    /// <summary>
    /// Marks the signal as covered by a work order draft.
    /// </summary>
    /// <param name="workOrderDraftId">The draft that covers the signal.</param>
    public void MarkDrafted(Guid workOrderDraftId)
    {
        Coverage = SignalCoverage.Drafted;
        WorkOrderDraftId = workOrderDraftId;
    }

    /// <summary>
    /// Marks the signal as covered by an existing work order.
    /// </summary>
    /// <param name="workOrderId">The work order that covers the signal.</param>
    public void MarkLinked(Guid workOrderId)
    {
        Coverage = SignalCoverage.Linked;
        WorkOrderId = workOrderId;
    }

    private void Copy(SignalChange change)
    {
        MetricDefinitionId = change.MetricDefinitionId;
        MetricKey = change.MetricKey;
        StatusDefinitionId = change.StatusDefinitionId;
        StatusCode = change.StatusCode;
        PhaseCode = change.PhaseCode;
        Value = change.Value;
        Limit = change.Limit;
        Z = change.Z;
        ForecastSeconds = change.ForecastSeconds;
        ResolvedAt = change.ResolvedAt;
        Sequence = change.Sequence;
    }
}
