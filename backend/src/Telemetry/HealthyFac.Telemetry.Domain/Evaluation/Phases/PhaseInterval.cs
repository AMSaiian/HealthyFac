using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Telemetry.Domain.Evaluation.Phases;

/// <summary>
/// A time span in which a machine had one operating phase (or no matching phase). The row is append-only: it has no
/// audit fields and no soft delete, and only its end changes, once, when the interval closes. The time without data
/// belongs to no interval.
/// </summary>
public class PhaseInterval : IEntity
{
    /// <summary>
    /// Opens an interval.
    /// </summary>
    /// <param name="machineRegistrationId">The registration of the machine. It never changes.</param>
    /// <param name="operatingPhaseId">The phase, or <see langword="null"/> for the unknown mode (no phase
    /// matches).</param>
    /// <param name="startedAt">The <c>sampled_at</c> time of the sample that opened the interval.</param>
    public PhaseInterval(Guid machineRegistrationId, Guid? operatingPhaseId, DateTime startedAt)
    {
        MachineRegistrationId = machineRegistrationId;
        OperatingPhaseId = operatingPhaseId;
        StartedAt = startedAt;
    }

    private PhaseInterval()
    {
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid MachineRegistrationId { get; private set; }

    /// <summary>The phase of the interval, or <see langword="null"/> for the unknown mode.</summary>
    public Guid? OperatingPhaseId { get; private set; }

    public DateTime StartedAt { get; private set; }

    /// <summary>The end of the interval, or <see langword="null"/> while it is open.</summary>
    public DateTime? EndedAt { get; private set; }

    /// <summary>
    /// Closes the interval. A time before the start gives an empty interval, because the clocks of a device and of the
    /// service can differ.
    /// </summary>
    /// <param name="endedAt">The end of the interval.</param>
    public void Close(DateTime endedAt) => EndedAt = endedAt < StartedAt ? StartedAt : endedAt;
}
