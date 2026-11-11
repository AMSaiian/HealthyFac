using System.Reflection;
using HealthyFac.Auth.Messaging;
using HealthyFac.Core.Application;
using HealthyFac.Core.Domain;
using HealthyFac.Core.Infrastructure;
using HealthyFac.Core.Messaging;
using HealthyFac.Core.Web;
using HealthyFac.Document.Messaging;
using HealthyFac.Insights.Messaging;
using HealthyFac.Shared.Application;
using HealthyFac.Shared.Domain;
using HealthyFac.Shared.Infrastructure;
using HealthyFac.Shared.Web;
using HealthyFac.Telemetry.Messaging;

namespace HealthyFac.Core.ArchitectureTests.Dependencies.Data;

public static class LayerDependencyTestData
{
    public static readonly string CoreDomain = CoreDomainAssembly.Instance.GetName().Name!;
    public static readonly string CoreApplication = CoreApplicationAssembly.Instance.GetName().Name!;
    public static readonly string CoreInfrastructure = CoreInfrastructureAssembly.Instance.GetName().Name!;
    public static readonly string CoreWeb = CoreWebAssembly.Instance.GetName().Name!;
    public static readonly string CoreMessaging = CoreMessagingAssembly.Instance.GetName().Name!;
    public static readonly string InsightsMessaging = InsightsMessagingAssembly.Instance.GetName().Name!;
    public static readonly string DocumentMessaging = DocumentMessagingAssembly.Instance.GetName().Name!;
    public static readonly string TelemetryMessaging = TelemetryMessagingAssembly.Instance.GetName().Name!;
    public static readonly string AuthMessaging = AuthMessagingAssembly.Instance.GetName().Name!;
    public const string AuthApplication = "HealthyFac.Auth.Application";
    public const string AuthInfrastructure = "HealthyFac.Auth.Infrastructure";
    public const string AuthWeb = "HealthyFac.Auth.Web";
    public static readonly string SharedDomain = SharedDomainAssembly.Instance.GetName().Name!;
    public static readonly string SharedApplication = SharedApplicationAssembly.Instance.GetName().Name!;
    public static readonly string SharedInfrastructure = SharedInfrastructureAssembly.Instance.GetName().Name!;
    public static readonly string SharedWeb = SharedWebAssembly.Instance.GetName().Name!;
    public const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";
    public const string AspNetCore = "Microsoft.AspNetCore";
    public const string Npgsql = "Npgsql";
    public const string LinqToDB = "LinqToDB";
    public const string OpenTelemetry = "OpenTelemetry";

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
        { CoreDomain, OpenTelemetry },
        { CoreApplication, OpenTelemetry },
        { CoreInfrastructure, OpenTelemetry },
        { SharedDomain, OpenTelemetry },
        { SharedApplication, OpenTelemetry },
        { SharedInfrastructure, OpenTelemetry },
        { CoreMessaging, CoreDomain },
        { CoreMessaging, CoreApplication },
        { CoreMessaging, CoreInfrastructure },
        { CoreMessaging, CoreWeb },
        { CoreMessaging, SharedInfrastructure },
        { CoreMessaging, SharedWeb },
        { CoreMessaging, AuthApplication },
        { CoreMessaging, AuthInfrastructure },
        { CoreMessaging, AuthWeb },
        { InsightsMessaging, CoreDomain },
        { InsightsMessaging, CoreApplication },
        { InsightsMessaging, CoreInfrastructure },
        { InsightsMessaging, CoreWeb },
        { InsightsMessaging, SharedInfrastructure },
        { InsightsMessaging, SharedWeb },
        { DocumentMessaging, CoreDomain },
        { DocumentMessaging, CoreApplication },
        { DocumentMessaging, CoreInfrastructure },
        { DocumentMessaging, CoreWeb },
        { DocumentMessaging, SharedInfrastructure },
        { DocumentMessaging, SharedWeb },
        { TelemetryMessaging, CoreDomain },
        { TelemetryMessaging, CoreApplication },
        { TelemetryMessaging, CoreInfrastructure },
        { TelemetryMessaging, CoreWeb },
        { TelemetryMessaging, SharedInfrastructure },
        { TelemetryMessaging, SharedWeb },
    };

    private static readonly Dictionary<string, Assembly> Labels = new[]
    {
        CoreDomainAssembly.Instance,
        CoreApplicationAssembly.Instance,
        CoreInfrastructureAssembly.Instance,
        CoreWebAssembly.Instance,
        CoreMessagingAssembly.Instance,
        InsightsMessagingAssembly.Instance,
        DocumentMessagingAssembly.Instance,
        TelemetryMessagingAssembly.Instance,
        AuthMessagingAssembly.Instance,
        SharedDomainAssembly.Instance,
        SharedApplicationAssembly.Instance,
        SharedInfrastructureAssembly.Instance,
        SharedWebAssembly.Instance,
    }.ToDictionary(assembly => assembly.GetName().Name!);

    /// <summary>
    /// Returns the assembly of a layer name. The names come from the assembly label classes, so a rename of a label
    /// breaks the build instead of changing what a test scans.
    /// </summary>
    /// <param name="name">The assembly name from this class.</param>
    /// <returns>The assembly that the label class of the name points to.</returns>
    public static Assembly Resolve(string name) => Labels[name];
}
