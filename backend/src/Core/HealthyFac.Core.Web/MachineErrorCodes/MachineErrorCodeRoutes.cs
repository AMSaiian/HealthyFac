namespace HealthyFac.Core.Web.MachineErrorCodes;

/// <summary>
/// Defines route constants for machine error code endpoints.
/// </summary>
public static class MachineErrorCodeRoutes
{
    public const string MachineIdParameter = "machineId";
    public const string IdParameter = "id";
    public const string BaseRoute = "/machines/{" + MachineIdParameter + "}/error-codes";
    public const string Root = "/";
    public const string ById = "/{" + IdParameter + "}";
    public const string Search = "/search";
    public const string ResourceLocationFormat = "{0}/{1}";
}
