namespace HealthyFac.Auth.ArchitectureTests.Dependencies.Data;

public static class LayerDependencyTestData
{
    public const string AuthApplication = "HealthyFac.Auth.Application";
    public const string AuthInfrastructure = "HealthyFac.Auth.Infrastructure";
    public const string AuthWeb = "HealthyFac.Auth.Web";
    public const string SharedApplication = "HealthyFac.Shared.Application";
    public const string SharedInfrastructure = "HealthyFac.Shared.Infrastructure";
    public const string SharedWeb = "HealthyFac.Shared.Web";
    public const string AspNetCore = "Microsoft.AspNetCore";
    public const string Npgsql = "Npgsql";
    public const string LinqToDB = "LinqToDB";
    public const string OpenTelemetry = "OpenTelemetry";

    public static TheoryData<string, string> ForbiddenDependencies => new()
    {
        { AuthApplication, AuthInfrastructure },
        { AuthApplication, AuthWeb },
        { AuthApplication, SharedInfrastructure },
        { AuthApplication, SharedWeb },
        { AuthApplication, AspNetCore },
        { AuthApplication, Npgsql },
        { AuthApplication, LinqToDB },
        { AuthInfrastructure, AuthWeb },
        { AuthInfrastructure, SharedWeb },
        { AuthInfrastructure, AspNetCore },
        { AuthWeb, LinqToDB },
        { AuthApplication, OpenTelemetry },
        { AuthInfrastructure, OpenTelemetry },
    };
}
