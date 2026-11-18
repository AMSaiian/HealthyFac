namespace HealthyFac.Telemetry.Application.DefinitionDrafts;

/// <summary>
/// Defines the kinds of definition draft that the draft grid and the API show. The camelCase values reach the API.
/// </summary>
public static class DefinitionDraftKinds
{
    public const string Metric = "metric";
    public const string Status = "status";

    public static readonly string[] All = [Metric, Status];
}
