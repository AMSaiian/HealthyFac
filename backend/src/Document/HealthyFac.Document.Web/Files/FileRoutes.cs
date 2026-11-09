namespace HealthyFac.Document.Web.Files;

/// <summary>
/// Defines route constants for file endpoints.
/// </summary>
public static class FileRoutes
{
    public const string MachineIdParameter = "machineId";
    public const string IdParameter = "id";
    public const string CompanyBaseRoute = "/files";
    public const string MachineBaseRoute = "/machines/{" + MachineIdParameter + "}/files";
    public const string Root = "/";
    public const string Embedded = "/embedded";
    public const string Search = "/search";
    public const string Links = "/links";
    public const string ById = "/{" + IdParameter + "}";
}
