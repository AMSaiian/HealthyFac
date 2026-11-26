using HealthyFac.Testing.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.MachineSimulator.IntegrationTests.Simulation.Data;

/// <summary>
/// Hosts the simulator with a fake clock and a log capture. The timers of the loops fire only when a test advances
/// the clock, so a test drives each loop on its own.
/// </summary>
public sealed class SimulatorFactory(
    IReadOnlyDictionary<string, string?> settings,
    FakeTimeProvider clock,
    CapturingLoggerProvider logs
) : WebApplicationFactory<Program>
{
    private readonly IReadOnlyDictionary<string, string?> _settings = settings;
    private readonly FakeTimeProvider _clock = clock;
    private readonly CapturingLoggerProvider _logs = logs;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(SimulatorTestData.EnvironmentName);

        foreach (var (key, value) in _settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(_settings));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(_clock);
            services.AddSingleton<ILoggerFactory>(new LoggerFactory([_logs]));
        });
    }
}
