using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Telemetry.Domain.MachineRegistrations;

/// <summary>
/// The Telemetry copy of a Core machine: the machine ID, the gateway ID, and the company. The values
/// never change, so one registration per machine is enough. A removal keeps the row (soft delete).
/// </summary>
public class MachineRegistration : IEntity, ISoftDeletable, IVersioned
{
    /// <summary>
    /// Creates the registration of a machine.
    /// </summary>
    /// <param name="id">The ID of the machine in Core. It is the ID of the registration.</param>
    /// <param name="gatewayId">The public identifier of the machine in MQTT messages.</param>
    /// <param name="companyId">The company of the machine.</param>
    /// <param name="expectedGoLiveOn">The expected go-live day of the machine in the company zone.</param>
    /// <param name="goLiveStartsAt">The UTC start of <paramref name="expectedGoLiveOn"/> in the company zone.</param>
    public MachineRegistration(
        Guid id,
        string gatewayId,
        Guid companyId,
        DateOnly expectedGoLiveOn,
        DateTime goLiveStartsAt
    )
    {
        Id = id;
        GatewayId = gatewayId;
        CompanyId = companyId;
        ExpectedGoLiveOn = expectedGoLiveOn;
        GoLiveStartsAt = goLiveStartsAt;
    }

    public Guid Id { get; private set; }
    public uint RowVersion { get; private set; }
    public string GatewayId { get; private set; }
    public Guid CompanyId { get; private set; }

    /// <summary>The number of the last applied set of metric definition changes. It starts at 0.</summary>
    public int MetricSchemaRevision { get; private set; }

    /// <summary>
    /// Raises the revision number by 1 for one apply of metric definition changes.
    /// </summary>
    /// <returns>The new revision number.</returns>
    public int AdvanceMetricSchemaRevision() => ++MetricSchemaRevision;

    /// <summary>The current state. A new registration is <see cref="MachineRegistrationState.Operational"/>.</summary>
    public MachineRegistrationState State { get; private set; } = MachineRegistrationState.Operational;

    /// <summary>The UTC start of the current state, or <see langword="null"/> before the first state message.</summary>
    public DateTime? StateStartedAt { get; private set; }

    /// <summary>The expected go-live day of the machine in the company zone.</summary>
    public DateOnly ExpectedGoLiveOn { get; private set; }

    /// <summary>The UTC start of <see cref="ExpectedGoLiveOn"/> in the company zone.</summary>
    public DateTime GoLiveStartsAt { get; private set; }

    /// <summary>The UTC time of the last go-live change message that the registration applied.</summary>
    public DateTime? GoLiveChangedAt { get; private set; }

    /// <summary>
    /// Applies a state message, unless the message is not newer than the stored state.
    /// </summary>
    /// <param name="state">The new state.</param>
    /// <param name="startedAt">The UTC start of the new state.</param>
    /// <returns><see langword="true"/> when the state changed, <see langword="false"/> for an older or equal start.
    /// </returns>
    public bool ChangeState(MachineRegistrationState state, DateTime startedAt)
    {
        if (StateStartedAt is not null && startedAt <= StateStartedAt)
        {
            return false;
        }

        State = state;
        StateStartedAt = startedAt;

        return true;
    }

    /// <summary>
    /// Applies a go-live change message, unless the message is not newer than the last applied one. A newer message
    /// stores its time even when its day equals the stored day, so an older message cannot overwrite it later.
    /// </summary>
    /// <param name="day">The new go-live day.</param>
    /// <param name="startsAt">The UTC start of the day in the company zone.</param>
    /// <param name="changedAt">The UTC time of the change in Core.</param>
    /// <returns><see langword="true"/> when the message was applied and the caller must save.</returns>
    public bool ChangeGoLive(DateOnly day, DateTime startsAt, DateTime changedAt)
    {
        if (GoLiveChangedAt is not null && changedAt <= GoLiveChangedAt)
        {
            return false;
        }

        if (day != ExpectedGoLiveOn)
        {
            ExpectedGoLiveOn = day;
            GoLiveStartsAt = startsAt;
        }

        GoLiveChangedAt = changedAt;

        return true;
    }

    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
}
