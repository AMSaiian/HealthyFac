namespace HealthyFac.Telemetry.Web.StatusDefinitions;

/// <summary>
/// Defines route constants for status definition endpoints.
/// </summary>
public static class StatusDefinitionRoutes
{
    public const string MachineIdParameter = "machineId";
    public const string IdParameter = "id";
    public const string BaseRoute = "/machines/{" + MachineIdParameter + "}/status-definitions";
    public const string Root = "/";
    public const string ById = "/{" + IdParameter + "}";
    public const string Search = "/search";
    public const string ResourceLocationFormat = "{0}/{1}";
}
