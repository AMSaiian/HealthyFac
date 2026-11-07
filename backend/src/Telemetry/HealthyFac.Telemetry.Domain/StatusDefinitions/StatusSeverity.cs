using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Telemetry.Domain.StatusDefinitions;

/// <summary>
/// Defines how serious a status is. The camelCase values reach the API and the database, so never rename one in
/// place.
/// </summary>
public sealed class StatusSeverity : Enumeration<StatusSeverity>
{
    public static readonly StatusSeverity Info = new(value: "info");
    public static readonly StatusSeverity Warning = new(value: "warning");
    public static readonly StatusSeverity Error = new(value: "error");

    private StatusSeverity(string value)
        : base(value)
    {
    }
}
