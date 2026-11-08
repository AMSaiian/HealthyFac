using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Telemetry.Domain.MachineRegistrations;

/// <summary>
/// The Telemetry copy of a Core machine: the machine ID, the gateway ID, and the company. The values
/// never change, so one registration per machine is enough. A removal keeps the row (soft delete).
/// </summary>
public class MachineRegistration : IEntity, ISoftDeletable
{
    /// <summary>
    /// Creates the registration of a machine.
    /// </summary>
    /// <param name="id">The ID of the machine in Core. It is the ID of the registration.</param>
    /// <param name="gatewayId">The public identifier of the machine in MQTT messages.</param>
    /// <param name="companyId">The company of the machine.</param>
    public MachineRegistration(Guid id, string gatewayId, Guid companyId)
    {
        Id = id;
        GatewayId = gatewayId;
        CompanyId = companyId;
    }

    public Guid Id { get; private set; }
    public string GatewayId { get; private set; }
    public Guid CompanyId { get; private set; }

    /// <summary>The number of the last applied set of metric definition changes. It starts at 0.</summary>
    public int MetricSchemaRevision { get; private set; }

    /// <summary>
    /// Raises the revision number by 1 for one apply of metric definition changes.
    /// </summary>
    /// <returns>The new revision number.</returns>
    public int AdvanceMetricSchemaRevision() => ++MetricSchemaRevision;

    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }
}
