using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Core.Application.Employees.SendEmployeeInviteEmail;
using HealthyFac.Core.Infrastructure;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.Infrastructure.Database.Initialization;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Email;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Testing.Containers;
using HealthyFac.Testing.Persistence;
using HealthyFac.Testing.Resetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HealthyFac.Core.IntegrationTests.Database.Data;

public sealed class CoreDatabaseFixture : IAsyncLifetime, IResetBeforeEachTest
{
    private readonly PostgreSqlTestDatabase _database = new();

    private DatabaseResetter _resetter = null!;

    private ServiceProvider _services = null!;

    public RecordingEmailSender EmailSender { get; } = new();

    public ConcurrentSaveGate SaveGate { get; } = new();

    public IServiceScopeFactory ScopeFactory =>
        new InitializationActorScopeFactory(_services.GetRequiredService<IServiceScopeFactory>());

    public IServiceScope CreateActorScope(ActorContext actor)
        => _services.GetRequiredService<IServiceScopeFactory>().CreateActorScope(actor);

    public IInviteTokenService InviteTokenService => _services.GetRequiredService<IInviteTokenService>();

    public async ValueTask InitializeAsync()
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
        _resetter = await DatabaseResetter.CreateAsync(
            _database.ConnectionString,
            scope.ServiceProvider.GetRequiredService<CoreDbContext>());
    }

    /// <summary>
    /// Deletes every row, clears the recorded emails, and seeds the required roles again. Roles are not kept, because
    /// the initializer tests change and soft-delete them.
    /// </summary>
    public async ValueTask ResetAsync()
    {
        await _resetter.ResetAsync();
        EmailSender.Reset();

        using var scope = ScopeFactory.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ICoreDbInitializer>().SeedRequiredDataAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }
}
