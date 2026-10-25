namespace HealthyFac.Core.ArchitectureTests.Dependencies.Data;

public static class LayerDependencyTestData
{
    public const string CoreDomain = "HealthyFac.Core.Domain";
    public const string CoreApplication = "HealthyFac.Core.Application";
    public const string CoreInfrastructure = "HealthyFac.Core.Infrastructure";
    public const string CoreWeb = "HealthyFac.Core.Web";
    public const string SharedDomain = "HealthyFac.Shared.Domain";
    public const string SharedApplication = "HealthyFac.Shared.Application";
    public const string SharedInfrastructure = "HealthyFac.Shared.Infrastructure";
    public const string SharedWeb = "HealthyFac.Shared.Web";
    public const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";
    public const string AspNetCore = "Microsoft.AspNetCore";
    public const string Npgsql = "Npgsql";
    public const string LinqToDB = "LinqToDB";

    public static TheoryData<string, string> ForbiddenDependencies => new()
    {
        { CoreDomain, CoreApplication },
        { CoreDomain, CoreInfrastructure },
        { CoreDomain, CoreWeb },
        { CoreDomain, SharedApplication },
        { CoreDomain, SharedInfrastructure },
        { CoreDomain, SharedWeb },
        { CoreDomain, EntityFrameworkCore },
        { CoreDomain, AspNetCore },
        { CoreDomain, Npgsql },
        { CoreDomain, LinqToDB },
        { CoreApplication, CoreInfrastructure },
        { CoreApplication, CoreWeb },
        { CoreApplication, SharedInfrastructure },
        { CoreApplication, SharedWeb },
        { CoreApplication, AspNetCore },
        { CoreApplication, Npgsql },
        { CoreApplication, LinqToDB },
        { CoreInfrastructure, CoreWeb },
        { CoreInfrastructure, SharedWeb },
        { CoreInfrastructure, AspNetCore },
        { CoreWeb, LinqToDB },
        { SharedDomain, LinqToDB },
        { SharedApplication, LinqToDB },
        { SharedWeb, LinqToDB },
    };
}
