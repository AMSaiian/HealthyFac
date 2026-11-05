using System.Reflection;
using HealthyFac.Shared.Infrastructure.CurrentUser.ServiceIdentity;

namespace HealthyFac.Shared.UnitTests.Observability.Data;

public static class TelemetryResourceTestData
{
    public const string ServiceName = "core";
    public const string EnvironmentName = "Development";
    public const string ExpectedEnvironmentName = "development";

    public const string OverriddenServiceName = "core-canary";
    public const string OverriddenEnvironmentName = "staging";

    public const string ServiceNameVariable = "OTEL_SERVICE_NAME";
    public const string ResourceAttributesVariable = "OTEL_RESOURCE_ATTRIBUTES";

    public static string ApplicationName => typeof(TelemetryResourceTestData).Assembly.GetName().Name!;

    public static string ExpectedVersion =>
        typeof(TelemetryResourceTestData).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
            .InformationalVersion;

    public static Dictionary<string, string?> Settings =>
        new()
        {
            [$"{ServiceOptions.SectionName}:Name"] = ServiceName,
        };

    public static Dictionary<string, string?> OverrideSettings =>
        new()
        {
            [$"{ServiceOptions.SectionName}:Name"] = ServiceName,
            [ServiceNameVariable] = OverriddenServiceName,
            [ResourceAttributesVariable] = $"deployment.environment.name={OverriddenEnvironmentName}",
        };
}
