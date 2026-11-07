namespace HealthyFac.Core.Infrastructure.Machines.Persistence;

/// <summary>
/// Defines the database index names of the machine table that code reads, so a name never differs between the
/// mapping and a check.
/// </summary>
public static class MachineIndexNames
{
    public const string CompanySerialNumber = "ix_machines_company_id_serial_number";
    public const string GatewayId = "ix_machines_gateway_id";
}
