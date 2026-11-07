namespace HealthyFac.Telemetry.ArchitectureTests.Dependencies.Data;

public static class LayerDependencyTestData
{
    public const string TelemetryDomain = "HealthyFac.Telemetry.Domain";
    public const string TelemetryApplication = "HealthyFac.Telemetry.Application";
    public const string TelemetryInfrastructure = "HealthyFac.Telemetry.Infrastructure";
    public const string TelemetryWeb = "HealthyFac.Telemetry.Web";
    public const string SharedDomain = "HealthyFac.Shared.Domain";
    public const string SharedApplication = "HealthyFac.Shared.Application";
    public const string SharedInfrastructure = "HealthyFac.Shared.Infrastructure";
    public const string SharedWeb = "HealthyFac.Shared.Web";
    public const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";
    public const string AspNetCore = "Microsoft.AspNetCore";
    public const string Npgsql = "Npgsql";
    public const string LinqToDB = "LinqToDB";
    public const string OpenTelemetry = "OpenTelemetry";
    public const string JsonSchema = "Json.Schema";

    public static TheoryData<string, string> ForbiddenDependencies => new()
    {
        { TelemetryDomain, TelemetryApplication },
        { TelemetryDomain, TelemetryInfrastructure },
        { TelemetryDomain, TelemetryWeb },
        { TelemetryDomain, SharedApplication },
        { TelemetryDomain, SharedInfrastructure },
        { TelemetryDomain, SharedWeb },
        { TelemetryDomain, EntityFrameworkCore },
        { TelemetryDomain, AspNetCore },
        { TelemetryDomain, Npgsql },
        { TelemetryDomain, LinqToDB },
        { TelemetryApplication, TelemetryInfrastructure },
        { TelemetryApplication, TelemetryWeb },
        { TelemetryApplication, SharedInfrastructure },
        { TelemetryApplication, SharedWeb },
        { TelemetryApplication, AspNetCore },
        { TelemetryApplication, Npgsql },
        { TelemetryApplication, LinqToDB },
        { TelemetryApplication, JsonSchema },
        { TelemetryDomain, JsonSchema },
        { TelemetryInfrastructure, TelemetryWeb },
        { TelemetryInfrastructure, SharedWeb },
        { TelemetryInfrastructure, AspNetCore },
        { TelemetryWeb, LinqToDB },
        { SharedDomain, LinqToDB },
        { SharedApplication, LinqToDB },
        { SharedWeb, LinqToDB },
        { TelemetryDomain, OpenTelemetry },
        { TelemetryApplication, OpenTelemetry },
        { TelemetryInfrastructure, OpenTelemetry },
        { SharedDomain, OpenTelemetry },
        { SharedApplication, OpenTelemetry },
        { SharedInfrastructure, OpenTelemetry },
    };
}
