namespace HealthyFac.Telemetry.Web.OperatingPhases;

/// <summary>
/// Defines route constants for operating phase endpoints.
/// </summary>
public static class OperatingPhaseRoutes
{
    public const string MachineIdParameter = "machineId";
    public const string IdParameter = "id";
    public const string BaseRoute = "/machines/{" + MachineIdParameter + "}/operating-phases";
    public const string Root = "/";
    public const string ById = "/{" + IdParameter + "}";
    public const string Order = "/order";
    public const string ResourceLocationFormat = "{0}/{1}";
}
