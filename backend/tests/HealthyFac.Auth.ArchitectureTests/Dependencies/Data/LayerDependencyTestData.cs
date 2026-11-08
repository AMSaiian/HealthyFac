using System.Reflection;
using HealthyFac.Auth.Application;
using HealthyFac.Auth.Infrastructure;
using HealthyFac.Auth.Messaging;
using HealthyFac.Auth.Web;
using HealthyFac.Core.Messaging;
using HealthyFac.Shared.Application;

namespace HealthyFac.Auth.ArchitectureTests.Dependencies.Data;

public static class LayerDependencyTestData
{
    public static readonly string AuthApplication = AuthApplicationAssembly.Instance.GetName().Name!;
    public static readonly string AuthInfrastructure = AuthInfrastructureAssembly.Instance.GetName().Name!;
    public static readonly string AuthWeb = AuthWebAssembly.Instance.GetName().Name!;
    public static readonly string AuthMessaging = AuthMessagingAssembly.Instance.GetName().Name!;
    public static readonly string CoreMessaging = CoreMessagingAssembly.Instance.GetName().Name!;
    public const string CoreApplication = "HealthyFac.Core.Application";
    public const string CoreInfrastructure = "HealthyFac.Core.Infrastructure";
    public const string CoreWeb = "HealthyFac.Core.Web";
    public static readonly string SharedApplication = SharedApplicationAssembly.Instance.GetName().Name!;
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
        { AuthMessaging, AuthApplication },
        { AuthMessaging, AuthInfrastructure },
        { AuthMessaging, AuthWeb },
        { AuthMessaging, SharedInfrastructure },
        { AuthMessaging, SharedWeb },
        { AuthMessaging, CoreApplication },
        { AuthMessaging, CoreInfrastructure },
        { AuthMessaging, CoreWeb },
    };

    private static readonly Dictionary<string, Assembly> Labels = new[]
    {
        AuthApplicationAssembly.Instance,
        AuthInfrastructureAssembly.Instance,
        AuthWebAssembly.Instance,
        AuthMessagingAssembly.Instance,
        CoreMessagingAssembly.Instance,
        SharedApplicationAssembly.Instance,
    }.ToDictionary(assembly => assembly.GetName().Name!);

    /// <summary>
    /// Returns the assembly of a layer name. The names come from the assembly label classes, so a rename of a label
    /// breaks the build instead of changing what a test scans.
    /// </summary>
    /// <param name="name">The assembly name from this class.</param>
    /// <returns>The assembly that the label class of the name points to.</returns>
    public static Assembly Resolve(string name) => Labels[name];
}
