namespace HealthyFac.Telemetry.Web.MetricDefinitions;

/// <summary>
/// Defines route constants for metric definition endpoints.
/// </summary>
public static class MetricDefinitionRoutes
{
    public const string MachineIdParameter = "machineId";
    public const string DefinitionsRoute = "/machines/{" + MachineIdParameter + "}/metric-definitions";
    public const string SchemaRoute = "/machines/{" + MachineIdParameter + "}/metric-schema";
}
