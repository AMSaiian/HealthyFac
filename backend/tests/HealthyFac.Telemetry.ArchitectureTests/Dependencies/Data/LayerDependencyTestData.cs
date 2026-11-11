using System.Reflection;
using HealthyFac.Core.Messaging;
using HealthyFac.Document.Messaging;
using HealthyFac.Insights.Messaging;
using HealthyFac.Shared.Application;
using HealthyFac.Shared.Domain;
using HealthyFac.Shared.Infrastructure;
using HealthyFac.Shared.Web;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Domain;
using HealthyFac.Telemetry.Infrastructure;
using HealthyFac.Telemetry.Messaging;
using HealthyFac.Telemetry.Web;

namespace HealthyFac.Telemetry.ArchitectureTests.Dependencies.Data;

public static class LayerDependencyTestData
{
    public static readonly string TelemetryDomain = TelemetryDomainAssembly.Instance.GetName().Name!;
    public static readonly string TelemetryApplication = TelemetryApplicationAssembly.Instance.GetName().Name!;
    public static readonly string TelemetryInfrastructure = TelemetryInfrastructureAssembly.Instance.GetName().Name!;
    public static readonly string TelemetryWeb = TelemetryWebAssembly.Instance.GetName().Name!;
    public static readonly string CoreMessaging = CoreMessagingAssembly.Instance.GetName().Name!;
    public static readonly string InsightsMessaging = InsightsMessagingAssembly.Instance.GetName().Name!;
    public static readonly string DocumentMessaging = DocumentMessagingAssembly.Instance.GetName().Name!;
    public static readonly string TelemetryMessaging = TelemetryMessagingAssembly.Instance.GetName().Name!;
    public static readonly string SharedDomain = SharedDomainAssembly.Instance.GetName().Name!;
    public static readonly string SharedApplication = SharedApplicationAssembly.Instance.GetName().Name!;
    public static readonly string SharedInfrastructure = SharedInfrastructureAssembly.Instance.GetName().Name!;
    public static readonly string SharedWeb = SharedWebAssembly.Instance.GetName().Name!;
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
        { InsightsMessaging, TelemetryDomain },
        { InsightsMessaging, TelemetryApplication },
        { InsightsMessaging, TelemetryInfrastructure },
        { InsightsMessaging, TelemetryWeb },
        { InsightsMessaging, SharedInfrastructure },
        { InsightsMessaging, SharedWeb },
        { DocumentMessaging, TelemetryDomain },
        { DocumentMessaging, TelemetryApplication },
        { DocumentMessaging, TelemetryInfrastructure },
        { DocumentMessaging, TelemetryWeb },
        { DocumentMessaging, SharedInfrastructure },
        { DocumentMessaging, SharedWeb },
        { TelemetryMessaging, TelemetryDomain },
        { TelemetryMessaging, TelemetryApplication },
        { TelemetryMessaging, TelemetryInfrastructure },
        { TelemetryMessaging, TelemetryWeb },
        { TelemetryMessaging, SharedInfrastructure },
        { TelemetryMessaging, SharedWeb },
    };

    private static readonly Dictionary<string, Assembly> Labels = new[]
    {
        TelemetryDomainAssembly.Instance,
        TelemetryApplicationAssembly.Instance,
        TelemetryInfrastructureAssembly.Instance,
        TelemetryWebAssembly.Instance,
        CoreMessagingAssembly.Instance,
        InsightsMessagingAssembly.Instance,
        DocumentMessagingAssembly.Instance,
        TelemetryMessagingAssembly.Instance,
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
