using System.Reflection;
using HealthyFac.Core.Messaging;
using HealthyFac.Document.Application;
using HealthyFac.Document.Contracts;
using HealthyFac.Document.Domain;
using HealthyFac.Document.Infrastructure;
using HealthyFac.Document.Messaging;
using HealthyFac.Document.Web;
using HealthyFac.Insights.Messaging;
using HealthyFac.Shared.Application;
using HealthyFac.Shared.Domain;
using HealthyFac.Shared.Infrastructure;
using HealthyFac.Shared.Web;
using HealthyFac.Telemetry.Messaging;

namespace HealthyFac.Document.ArchitectureTests.Dependencies.Data;

public static class LayerDependencyTestData
{
    public static readonly string DocumentDomain = DocumentDomainAssembly.Instance.GetName().Name!;
    public static readonly string DocumentApplication = DocumentApplicationAssembly.Instance.GetName().Name!;
    public static readonly string DocumentInfrastructure = DocumentInfrastructureAssembly.Instance.GetName().Name!;
    public static readonly string DocumentWeb = DocumentWebAssembly.Instance.GetName().Name!;
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
    public const string FileSignatures = "FileSignatures";
    public const string AzureStorage = "Azure.Storage";

    public static TheoryData<string, string> ForbiddenDependencies => new()
    {
        { DocumentDomain, DocumentApplication },
        { DocumentDomain, DocumentInfrastructure },
        { DocumentDomain, DocumentWeb },
        { DocumentDomain, SharedApplication },
        { DocumentDomain, SharedInfrastructure },
        { DocumentDomain, SharedWeb },
        { DocumentDomain, EntityFrameworkCore },
        { DocumentDomain, AspNetCore },
        { DocumentDomain, Npgsql },
        { DocumentDomain, LinqToDB },
        { DocumentApplication, DocumentInfrastructure },
        { DocumentApplication, DocumentWeb },
        { DocumentApplication, SharedInfrastructure },
        { DocumentApplication, SharedWeb },
        { DocumentApplication, AspNetCore },
        { DocumentApplication, Npgsql },
        { DocumentApplication, LinqToDB },
        { DocumentInfrastructure, DocumentWeb },
        { DocumentInfrastructure, SharedWeb },
        { DocumentInfrastructure, AspNetCore },
        { DocumentWeb, LinqToDB },
        { SharedDomain, LinqToDB },
        { SharedApplication, LinqToDB },
        { SharedWeb, LinqToDB },
        { DocumentDomain, OpenTelemetry },
        { DocumentDomain, AzureStorage },
        { DocumentApplication, FileSignatures },
        { DocumentApplication, AzureStorage },
        { DocumentApplication, OpenTelemetry },
        { DocumentInfrastructure, OpenTelemetry },
        { SharedDomain, OpenTelemetry },
        { SharedApplication, OpenTelemetry },
        { SharedInfrastructure, OpenTelemetry },
        { InsightsMessaging, DocumentDomain },
        { InsightsMessaging, DocumentApplication },
        { InsightsMessaging, DocumentInfrastructure },
        { InsightsMessaging, DocumentWeb },
        { InsightsMessaging, SharedInfrastructure },
        { InsightsMessaging, SharedWeb },
        { DocumentMessaging, DocumentDomain },
        { DocumentMessaging, DocumentApplication },
        { DocumentMessaging, DocumentInfrastructure },
        { DocumentMessaging, DocumentWeb },
        { DocumentMessaging, SharedInfrastructure },
        { DocumentMessaging, SharedWeb },
        { TelemetryMessaging, DocumentDomain },
        { TelemetryMessaging, DocumentApplication },
        { TelemetryMessaging, DocumentInfrastructure },
        { TelemetryMessaging, DocumentWeb },
        { TelemetryMessaging, SharedInfrastructure },
        { TelemetryMessaging, SharedWeb },
        { DocumentContracts, DocumentDomain },
        { DocumentContracts, DocumentApplication },
        { DocumentContracts, DocumentInfrastructure },
        { DocumentContracts, DocumentWeb },
        { DocumentContracts, SharedApplication },
        { DocumentContracts, SharedInfrastructure },
        { DocumentContracts, SharedWeb },
    };

    private static readonly Dictionary<string, Assembly> Labels = new[]
    {
        DocumentDomainAssembly.Instance,
        DocumentApplicationAssembly.Instance,
        DocumentInfrastructureAssembly.Instance,
        DocumentWebAssembly.Instance,
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
