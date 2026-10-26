using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Core.IntegrationTests.Database.Data;

public sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = DatabaseTestData.EnvironmentName;
    public string ApplicationName { get; set; } = DatabaseTestData.ApplicationName;
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

public sealed class TransactionTestException : Exception;

/// <summary>
/// Creates scopes that already hold the system actor, so each test step writes with a known actor.
/// </summary>
/// <param name="inner">The scope factory of the test service provider.</param>
public sealed class SystemActorScopeFactory(IServiceScopeFactory inner) : IServiceScopeFactory
{
    public IServiceScope CreateScope() => inner.CreateActorScope(SystemActor.Create());
}
