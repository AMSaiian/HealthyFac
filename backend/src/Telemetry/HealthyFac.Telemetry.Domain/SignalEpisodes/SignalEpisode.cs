using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Telemetry.Domain.SignalEpisodes;

/// <summary>
/// One period in which a rule held for a machine, for example a temperature above its alarm limit. The episode opens
/// with a severity, may escalate once from <see cref="SignalSeverity.Warning"/> to
/// <see cref="SignalSeverity.Alarm"/>, and resolves once. Every change raises <see cref="Sequence"/> by 1, so a
/// consumer drops an old message. The times are the <c>sampled_at</c> times of the samples that caused the change.
/// </summary>
public class SignalEpisode : IEntity, IVersioned
{
    private SignalEpisode()
    {
        Rule = null!;
        Severity = null!;
        State = null!;
        Snapshot = SignalSnapshot.Empty;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public uint RowVersion { get; private set; }

    public Guid MachineRegistrationId { get; private set; }

    public Guid CompanyId { get; private set; }

    public SignalRule Rule { get; private set; }

    /// <summary>The metric of a metric rule, otherwise <see langword="null"/>.</summary>
    public Guid? MetricDefinitionId { get; private set; }

    /// <summary>The status of the status rule, otherwise <see langword="null"/>.</summary>
    public Guid? StatusDefinitionId { get; private set; }

    public SignalSeverity Severity { get; private set; }

    public SignalEpisodeState State { get; private set; }

    public DateTime OpenedAt { get; private set; }

    public DateTime? EscalatedAt { get; private set; }

    public DateTime? ResolvedAt { get; private set; }

    /// <summary>1 at the open, and 1 more for each escalate and for the resolve.</summary>
    public int Sequence { get; private set; }

    public Guid? OperatingPhaseId { get; private set; }

    /// <summary>The numbers of the latest change.</summary>
    public SignalSnapshot Snapshot { get; private set; }

    /// <summary>
    /// Opens an episode.
    /// </summary>
    /// <param name="machineRegistrationId">The machine.</param>
    /// <param name="companyId">The company of the machine.</param>
    /// <param name="rule">The rule that opened the episode.</param>
    /// <param name="metricDefinitionId">The metric of a metric rule.</param>
    /// <param name="statusDefinitionId">The status of the status rule.</param>
    /// <param name="severity">The severity at the open.</param>
    /// <param name="openedAt">The <c>sampled_at</c> time of the sample that opened the episode.</param>
    /// <param name="operatingPhaseId">The phase of the sample, or <see langword="null"/>.</param>
    /// <param name="snapshot">The numbers of the open.</param>
    /// <returns>The open episode with the sequence 1.</returns>
    public static SignalEpisode Open(
        Guid machineRegistrationId,
        Guid companyId,
        SignalRule rule,
        Guid? metricDefinitionId,
        Guid? statusDefinitionId,
        SignalSeverity severity,
        DateTime openedAt,
        Guid? operatingPhaseId,
        SignalSnapshot snapshot
    ) =>
        new()
        {
            MachineRegistrationId = machineRegistrationId,
            CompanyId = companyId,
            Rule = rule,
            MetricDefinitionId = metricDefinitionId,
            StatusDefinitionId = statusDefinitionId,
            Severity = severity,
            State = SignalEpisodeState.Open,
            OpenedAt = openedAt,
            Sequence = 1,
            OperatingPhaseId = operatingPhaseId,
            Snapshot = snapshot,
        };

    /// <summary>
    /// Raises the severity from warning to alarm.
    /// </summary>
    /// <param name="at">The <c>sampled_at</c> time of the sample that escalated the episode.</param>
    /// <param name="snapshot">The numbers of the escalate.</param>
    /// <exception cref="InvalidOperationException">The episode is not an open warning.</exception>
    public void Escalate(DateTime at, SignalSnapshot snapshot)
    {
        if (State != SignalEpisodeState.Open || Severity != SignalSeverity.Warning)
        {
            throw SignalEpisodeInvariantErrors.EscalateOnlyFromOpenWarning();
        }

        Severity = SignalSeverity.Alarm;
        EscalatedAt = at;
        Snapshot = snapshot;
        Sequence++;
    }

    /// <summary>
    /// Resolves the episode.
    /// </summary>
    /// <param name="at">The <c>sampled_at</c> time of the sample that resolved the episode.</param>
    /// <param name="snapshot">The numbers of the resolve.</param>
    /// <exception cref="InvalidOperationException">The episode is not open.</exception>
    public void Resolve(DateTime at, SignalSnapshot snapshot)
    {
        if (State != SignalEpisodeState.Open)
        {
            throw SignalEpisodeInvariantErrors.ResolveOnlyOnce();
        }

        State = SignalEpisodeState.Resolved;
        ResolvedAt = at;
        Snapshot = snapshot;
        Sequence++;
    }
}
