using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Domain.SignalEpisodes;

namespace HealthyFac.Telemetry.Domain.Evaluation.Rules;

/// <summary>
/// Counts the samples in a row for which a rule condition holds, for one subject (a metric or a status) of one
/// machine. A rule opens, escalates, or resolves only after enough samples in a row, so a single outlier changes
/// nothing. Samples are counted in the order of their <c>sampled_at</c> time, so a split of a backlog into several
/// runs gives the same result.
/// </summary>
public class RuleStreak : IEntity
{
    public RuleStreak(Guid machineRegistrationId, SignalRule rule, Guid? subjectId)
    {
        MachineRegistrationId = machineRegistrationId;
        Rule = rule;
        SubjectId = subjectId;
    }

    private RuleStreak()
    {
        Rule = null!;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid MachineRegistrationId { get; private set; }

    public SignalRule Rule { get; private set; }

    /// <summary>The metric or the status definition of the subject, or <see langword="null"/> for a rule of the
    /// machine.</summary>
    public Guid? SubjectId { get; private set; }

    public int OpenCount { get; private set; }

    public int EscalateCount { get; private set; }

    public int ResolveCount { get; private set; }

    /// <summary>The sample time of the first sample of the current open streak, or <see langword="null"/> when
    /// the open count is 0.</summary>
    public DateTime? OpenSince { get; private set; }

    /// <summary>The sample time of the first sample of the current resolve streak, or <see langword="null"/>
    /// when the resolve count is 0.</summary>
    public DateTime? ResolveSince { get; private set; }

    /// <summary>A sample before this time does not count, for example while the effective limits settle after a phase
    /// switch.</summary>
    public DateTime? CountsFrom { get; private set; }

    /// <summary>
    /// Sets the time from which samples count and restarts the count, so a sample from before the change never adds
    /// to a streak after it.
    /// </summary>
    /// <param name="countsFrom">The time, or <see langword="null"/> for every sample.</param>
    public void CountFrom(DateTime? countsFrom)
    {
        CountsFrom = countsFrom;
        OpenCount = 0;
        EscalateCount = 0;
        ResolveCount = 0;
        OpenSince = null;
        ResolveSince = null;
    }

    /// <summary>
    /// Counts one sample and decides.
    /// </summary>
    /// <param name="condition">The conditions that hold for the sample.</param>
    /// <param name="openSeverity">The severity of the open episode of the subject, or <see langword="null"/> when
    /// there is none.</param>
    /// <param name="at">The <c>sampled_at</c> time of the sample.</param>
    /// <param name="triggerPersistence">The samples in a row that open or escalate.</param>
    /// <param name="resolvePersistence">The samples in a row that resolve.</param>
    /// <param name="snapshot">The numbers of the sample.</param>
    /// <param name="minimumDuration">The time that the open or the resolve streak must also last, from its first
    /// sample, or <see langword="null"/> for the count alone.</param>
    /// <returns>The decision. A sample before <see cref="CountsFrom"/> gives none and counts nothing.</returns>
    public RuleDecision Observe(
        RuleCondition condition,
        SignalSeverity? openSeverity,
        DateTime at,
        int triggerPersistence,
        int resolvePersistence,
        SignalSnapshot snapshot,
        TimeSpan? minimumDuration = null
    )
    {
        if (CountsFrom is { } from && at < from)
        {
            return RuleDecision.None;
        }

        OpenCount = condition.IsOpen ? OpenCount + 1 : 0;
        EscalateCount = condition.IsAlarm ? EscalateCount + 1 : 0;
        ResolveCount = condition.IsResolved ? ResolveCount + 1 : 0;
        OpenSince = condition.IsOpen ? OpenSince ?? at : null;
        ResolveSince = condition.IsResolved ? ResolveSince ?? at : null;
        var minimum = minimumDuration ?? TimeSpan.Zero;

        if (openSeverity is null)
        {
            return Open(triggerPersistence, snapshot, at, minimum);
        }

        if (openSeverity == SignalSeverity.Warning && EscalateCount >= triggerPersistence)
        {
            EscalateCount = 0;

            return new RuleDecision
            {
                Kind = RuleDecisionKind.Escalate,
                Severity = SignalSeverity.Alarm,
                Snapshot = snapshot,
            };
        }

        if (ResolveCount >= resolvePersistence && IsLongEnough(ResolveSince, at, minimum))
        {
            ResolveCount = 0;
            ResolveSince = null;

            return new RuleDecision { Kind = RuleDecisionKind.Resolve, Snapshot = snapshot };
        }

        return RuleDecision.None;
    }

    private static bool IsLongEnough(DateTime? since, DateTime at, TimeSpan minimum) =>
        minimum == TimeSpan.Zero || at - since >= minimum;

    private RuleDecision Open(int triggerPersistence, SignalSnapshot snapshot, DateTime at, TimeSpan minimum)
    {
        var isLongEnough = IsLongEnough(OpenSince, at, minimum);

        var severity =
            EscalateCount >= triggerPersistence && isLongEnough
                ? SignalSeverity.Alarm
                : OpenCount >= triggerPersistence && isLongEnough
                    ? SignalSeverity.Warning
                    : null;

        if (severity is null)
        {
            return RuleDecision.None;
        }

        OpenCount = 0;
        EscalateCount = 0;
        OpenSince = null;

        return new RuleDecision { Kind = RuleDecisionKind.Open, Severity = severity, Snapshot = snapshot };
    }
}

/// <summary>
/// The conditions of a rule for one sample: the condition that opens a warning, the condition that opens or
/// escalates to an alarm, and the condition that resolves.
/// </summary>
public readonly record struct RuleCondition
{
    /// <summary>The sample opens a warning.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>The sample opens or escalates to an alarm.</summary>
    public required bool IsAlarm { get; init; }

    /// <summary>The sample is in the resolve state.</summary>
    public required bool IsResolved { get; init; }
}
