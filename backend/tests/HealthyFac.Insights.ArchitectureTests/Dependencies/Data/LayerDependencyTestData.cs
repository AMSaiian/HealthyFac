using System.Reflection;
using HealthyFac.Core.Messaging;
using HealthyFac.Document.Contracts;
using HealthyFac.Document.Messaging;
using HealthyFac.Insights.Application;
using HealthyFac.Insights.Domain;
using HealthyFac.Insights.Infrastructure;
using HealthyFac.Insights.Messaging;
using HealthyFac.Insights.Web;
using HealthyFac.Shared.Application;
using HealthyFac.Shared.Domain;
using HealthyFac.Shared.Infrastructure;
using HealthyFac.Shared.Web;
using HealthyFac.Telemetry.Messaging;

namespace HealthyFac.Insights.ArchitectureTests.Dependencies.Data;

public static class LayerDependencyTestData
{
    public static readonly string InsightsDomain = InsightsDomainAssembly.Instance.GetName().Name!;
    public static readonly string InsightsApplication = InsightsApplicationAssembly.Instance.GetName().Name!;
    public static readonly string InsightsInfrastructure = InsightsInfrastructureAssembly.Instance.GetName().Name!;
    public static readonly string InsightsWeb = InsightsWebAssembly.Instance.GetName().Name!;
    public static readonly string CoreMessaging = CoreMessagingAssembly.Instance.GetName().Name!;
    public static readonly string InsightsMessaging = InsightsMessagingAssembly.Instance.GetName().Name!;
    public static readonly string DocumentMessaging = DocumentMessagingAssembly.Instance.GetName().Name!;
    public static readonly string TelemetryMessaging = TelemetryMessagingAssembly.Instance.GetName().Name!;
    public static readonly string DocumentContracts = DocumentContractsAssembly.Instance.GetName().Name!;
    public static readonly string SharedDomain = SharedDomainAssembly.Instance.GetName().Name!;
    public static readonly string SharedApplication = SharedApplicationAssembly.Instance.GetName().Name!;
    public static readonly string SharedInfrastructure = SharedInfrastructureAssembly.Instance.GetName().Name!;
    public static readonly string SharedWeb = SharedWebAssembly.Instance.GetName().Name!;
    public const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";
    public const string AspNetCore = "Microsoft.AspNetCore";
    public const string Npgsql = "Npgsql";
    public const string LinqToDB = "LinqToDB";
    public const string OpenTelemetry = "OpenTelemetry";
    public const string GoogleGenAi = "Google.GenAI";
    public const string AgentFramework = "Microsoft.Agents";
    public const string GrpcClient = "Grpc.Net";

    public static TheoryData<string, string> ForbiddenDependencies => new()
    {
        { InsightsDomain, InsightsApplication },
        { InsightsDomain, InsightsInfrastructure },
        { InsightsDomain, InsightsWeb },
        { InsightsDomain, SharedApplication },
        { InsightsDomain, SharedInfrastructure },
        { InsightsDomain, SharedWeb },
        { InsightsDomain, EntityFrameworkCore },
        { InsightsDomain, AspNetCore },
        { InsightsDomain, Npgsql },
        { InsightsDomain, LinqToDB },
        { InsightsApplication, InsightsInfrastructure },
        { InsightsApplication, InsightsWeb },
        { InsightsApplication, SharedInfrastructure },
        { InsightsApplication, SharedWeb },
        { InsightsApplication, AspNetCore },
        { InsightsApplication, Npgsql },
        { InsightsApplication, LinqToDB },
        { InsightsInfrastructure, InsightsWeb },
        { InsightsInfrastructure, SharedWeb },
        { InsightsInfrastructure, AspNetCore },
        { InsightsWeb, LinqToDB },
        { SharedDomain, LinqToDB },
        { SharedApplication, LinqToDB },
        { SharedWeb, LinqToDB },
        { InsightsDomain, OpenTelemetry },
        { InsightsDomain, GoogleGenAi },
        { InsightsDomain, AgentFramework },
        { InsightsApplication, GoogleGenAi },
        { InsightsApplication, AgentFramework },
        { InsightsApplication, GrpcClient },
        { InsightsApplication, OpenTelemetry },
        { InsightsInfrastructure, OpenTelemetry },
        { SharedDomain, OpenTelemetry },
        { SharedApplication, OpenTelemetry },
        { SharedInfrastructure, OpenTelemetry },
        { InsightsMessaging, InsightsDomain },
        { InsightsMessaging, InsightsApplication },
        { InsightsMessaging, InsightsInfrastructure },
        { InsightsMessaging, InsightsWeb },
        { InsightsMessaging, SharedInfrastructure },
        { InsightsMessaging, SharedWeb },
        { DocumentMessaging, InsightsDomain },
        { DocumentMessaging, InsightsApplication },
        { DocumentMessaging, InsightsInfrastructure },
        { DocumentMessaging, InsightsWeb },
        { DocumentMessaging, SharedInfrastructure },
        { DocumentMessaging, SharedWeb },
        { TelemetryMessaging, InsightsDomain },
        { TelemetryMessaging, InsightsApplication },
        { TelemetryMessaging, InsightsInfrastructure },
        { TelemetryMessaging, InsightsWeb },
        { TelemetryMessaging, SharedInfrastructure },
        { TelemetryMessaging, SharedWeb },
        { DocumentContracts, InsightsDomain },
        { DocumentContracts, InsightsApplication },
        { DocumentContracts, InsightsInfrastructure },
        { DocumentContracts, InsightsWeb },
        { DocumentContracts, SharedApplication },
        { DocumentContracts, SharedInfrastructure },
        { DocumentContracts, SharedWeb },
    };

    private static readonly Dictionary<string, Assembly> Labels = new[]
    {
        InsightsDomainAssembly.Instance,
        InsightsApplicationAssembly.Instance,
        InsightsInfrastructureAssembly.Instance,
        InsightsWebAssembly.Instance,
        CoreMessagingAssembly.Instance,
        InsightsMessagingAssembly.Instance,
        DocumentMessagingAssembly.Instance,
        TelemetryMessagingAssembly.Instance,
        DocumentContractsAssembly.Instance,
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
