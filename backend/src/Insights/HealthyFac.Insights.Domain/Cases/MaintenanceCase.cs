using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Insights.Domain.Cases;

/// <summary>
/// The open question of the maintenance of one machine: the signals that belong together, and the work that answers
/// them. A machine has at most one open case. The case is <c>open</c> while it has an open signal, becomes
/// <c>resolved</c> when no signal is open, and becomes <c>closed</c> when a user or the agent closes it.
/// </summary>
public class MaintenanceCase : IEntity, IAuditable, IVersioned
{
    private readonly List<MachineSignal> _signals = [];

    /// <summary>
    /// Creates an open case.
    /// </summary>
    /// <param name="companyId">The company of the machine.</param>
    /// <param name="machineId">The machine of the case.</param>
    /// <param name="openedAt">The time of the first signal (UTC).</param>
    public MaintenanceCase(Guid companyId, Guid machineId, DateTime openedAt)
    {
        CompanyId = companyId;
        MachineId = machineId;
        OpenedAt = openedAt;
        State = CaseState.Open;
    }

    private MaintenanceCase()
    {
        State = null!;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public uint RowVersion { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid MachineId { get; private set; }

    public CaseState State { get; private set; }

    public DateTime OpenedAt { get; private set; }

    public DateTime? ClosedAt { get; private set; }

    public string? CloseReason { get; private set; }

    public DateTime CreatedAt { get; }

    public DateTime? UpdatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    /// <summary>The signals of the case. A query loads them with <c>Include</c>.</summary>
    public IReadOnlyCollection<MachineSignal> Signals => _signals;

    /// <summary>
    /// Puts a signal into the case.
    /// </summary>
    /// <param name="signal">The signal.</param>
    public void Add(MachineSignal signal)
    {
        signal.JoinCase(Id);
        _signals.Add(signal);
    }

    /// <summary>
    /// Closes the case. An uncovered signal (<c>new</c> or <c>watched</c>) becomes <c>dismissed</c>. A <c>drafted</c>
    /// and a <c>linked</c> signal keep their coverage, because a draft or a work order covers them.
    /// </summary>
    /// <param name="reason">The reason of the close.</param>
    /// <param name="now">The time of the close (UTC).</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown by <see cref="CaseInvariantErrors"/> when the case is <c>closed</c>.
    /// </exception>
    public void Close(string reason, DateTime now)
    {
        if (State == CaseState.Closed)
        {
            throw CaseInvariantErrors.IllegalTransition(Id, State);
        }

        foreach (var signal in _signals.Where(candidate => candidate.Coverage.IsUncovered))
        {
            signal.Dismiss();
        }

        State = CaseState.Closed;
        ClosedAt = now;
        CloseReason = reason;
    }

    /// <summary>
    /// Makes the case <c>resolved</c> when no signal of the case is open. An already resolved or closed case stays.
    /// </summary>
    /// <param name="now">The time of the resolve (UTC).</param>
    /// <returns><see langword="true"/> when the case became resolved.</returns>
    public bool ResolveIfDone(DateTime now)
    {
        if (State != CaseState.Open || _signals.Any(signal => signal.IsOpen))
        {
            return false;
        }

        State = CaseState.Resolved;
        ClosedAt = now;

        return true;
    }
}
