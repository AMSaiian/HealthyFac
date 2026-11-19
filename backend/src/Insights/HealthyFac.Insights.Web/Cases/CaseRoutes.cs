namespace HealthyFac.Insights.Web.Cases;

/// <summary>
/// Defines route constants for the case endpoints.
/// </summary>
public static class CaseRoutes
{
    public const string BaseRoute = "/cases";
    public const string IdParameter = "id";
    public const string Search = "/search";
    public const string ById = "/{" + IdParameter + "}";
    public const string Runs = ById + "/runs";
    public const string Close = ById + "/close";
    public const string RunIdParameter = "runId";
    public const string SignalsSearch = ById + "/signals/search";
    public const string RunsSearch = ById + "/runs/search";
    public const string RunToolCalls = ById + "/runs/{" + RunIdParameter + "}/tool-calls";
}
