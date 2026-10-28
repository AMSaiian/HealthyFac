using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Core.Application.Employees.SendEmployeeInviteEmail;
using HealthyFac.Core.Infrastructure;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Shared.Application.Email;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Persistence;

namespace HealthyFac.Core.IntegrationTests.Database.Data;

public sealed class CoreDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlTestDatabase _database = new();

    private ServiceProvider _services = null!;

    public RecordingEmailSender EmailSender { get; } = new();

    public ConcurrentSaveGate SaveGate { get; } = new();

    public IServiceScopeFactory ScopeFactory =>
        new SystemActorScopeFactory(_services.GetRequiredService<IServiceScopeFactory>());

    public IInviteTokenService InviteTokenService => _services.GetRequiredService<IInviteTokenService>();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(DatabaseTestData.SettingsFileName, optional: false)
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [DatabaseTestData.ConnectionStringKey] = _database.ConnectionString,
                })
            .Build();

        var environment = new TestHostEnvironment();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddActorContext();
        services.AddApplicationServices();
        services.AddInfrastructureServices(configuration, environment);
        services.ConfigureDbContext<CoreDbContext>(options => options.AddInterceptors(SaveGate));
        services.AddSingleton<IEmailSender>(EmailSender);
        services.AddScoped<IEmployeeInviteEmailBuilder, PlainEmployeeInviteEmailBuilder>();

        _services = services.BuildServiceProvider(validateScopes: true);

        using var scope = ScopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CoreDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<ICoreDbInitializer>().SeedRequiredDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }
}
