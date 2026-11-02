using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Web.Tracing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace HealthyFac.Shared.UnitTests.Observability.Data;

/// <summary>
/// Builds the shared tracer provider from in-memory settings and returns its resource as a dictionary. The settings
/// stand in for the environment variables, because the SDK detectors read them through <see cref="IConfiguration"/>.
/// </summary>
public static class TelemetryResourceHarness
{
    public static IReadOnlyDictionary<string, object> BuildTracerResource(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<IHostEnvironment>(
                new HostingEnvironment
                {
                    EnvironmentName = TelemetryResourceTestData.EnvironmentName,
                    ApplicationName = TelemetryResourceTestData.ApplicationName,
                });
        services.AddServiceOptions(configuration);
        services.AddSharedTracing(configuration);

        using var provider = services.BuildServiceProvider();
        var tracerProvider = provider.GetRequiredService<TracerProvider>();

        return tracerProvider.GetResource().Attributes.ToDictionary();
    }
}
