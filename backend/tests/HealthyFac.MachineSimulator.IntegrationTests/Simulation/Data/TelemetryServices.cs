using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.MachineSimulator.IntegrationTests.Simulation.Data;

/// <summary>
/// Builds the Telemetry service provider of the fixture. It lives in its own file because the Core and the Telemetry
/// layers use the same extension names in different namespaces.
/// </summary>
public static class TelemetryServices
{
    public static ServiceProvider Build(IConfiguration configuration, IHostEnvironment environment)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(environment);
        services.AddActorContext();
        services.AddApplicationServices();
        services.AddInfrastructureServices(configuration, environment);

        return services.BuildServiceProvider(validateScopes: true);
    }
}
