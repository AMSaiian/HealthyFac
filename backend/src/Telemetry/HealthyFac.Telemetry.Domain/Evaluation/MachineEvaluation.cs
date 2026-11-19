using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Domain.Evaluation.Phases;

namespace HealthyFac.Telemetry.Domain.Evaluation;

/// <summary>
/// The evaluation state of one machine: when the next run is due, which worker owns the machine now, how far the
/// evaluation has read, and the phase tracking. The ID is the ID of the machine registration. Several workers take
/// machines from this table, so the lease and the row version keep one run per machine.
/// </summary>
public class MachineEvaluation : IEntity, IVersioned
{
    /// <summary>
    /// Creates the evaluation state of a registered machine.
    /// </summary>
    /// <param name="machineRegistrationId">The registration of the machine. It is the ID of this row.</param>
    /// <param name="dueAt">The time of the first run.</param>
    public MachineEvaluation(Guid machineRegistrationId, DateTime dueAt)
    {
        Id = machineRegistrationId;
        DueAt = dueAt;
    }

    private MachineEvaluation()
    {
    }

    public Guid Id { get; private set; }

    public uint RowVersion { get; private set; }

    public DateTime DueAt { get; private set; }

    /// <summary>The time until which a worker owns the machine. A later worker takes over an expired lease.</summary>
    public DateTime? LeaseUntil { get; private set; }

    /// <summary>The <c>sampled_at</c> time of the last evaluated sample.</summary>
    public DateTime? Watermark { get; private set; }

    public DateTime? LastRunAt { get; private set; }

    public PhaseTracking PhaseTracking { get; private set; } = new();

    /// <summary>
    /// Gives the machine to a worker.
    /// </summary>
    /// <param name="leaseUntil">The end of the lease.</param>
    public void Claim(DateTime leaseUntil) => LeaseUntil = leaseUntil;

    /// <summary>
    /// Stores the result of a run and releases the lease.
    /// </summary>
    /// <param name="watermark">The <c>sampled_at</c> time of the last evaluated sample.</param>
    /// <param name="now">The time of the run.</param>
    /// <param name="dueAt">The time of the next run.</param>
    public void CompleteRun(DateTime? watermark, DateTime now, DateTime dueAt)
    {
        Watermark = watermark ?? Watermark;
        LastRunAt = now;
        DueAt = dueAt;
        LeaseUntil = null;
    }
}
