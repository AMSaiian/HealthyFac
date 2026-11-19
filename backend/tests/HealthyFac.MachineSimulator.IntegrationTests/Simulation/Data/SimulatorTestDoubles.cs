using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.MachineSimulator.IntegrationTests.Simulation.Data;

public sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = SimulatorTestData.EnvironmentName;
    public string ApplicationName { get; set; } = SimulatorTestData.ApplicationName;
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

/// <summary>
/// Creates scopes that already hold the database initialization actor of one service, so each arrange step writes with
/// a known actor.
/// </summary>
/// <param name="inner">The scope factory of the test service provider.</param>
/// <param name="service">The service that owns the provider.</param>
public sealed class InitializationActorScopeFactory(IServiceScopeFactory inner, ServiceName service)
    : IServiceScopeFactory
{
    private readonly IServiceScopeFactory _inner = inner;
    private readonly ServiceName _service = service;

    public IServiceScope CreateScope() =>
        _inner.CreateActorScope(ServiceActor.DatabaseInitialization.For(_service));
}
