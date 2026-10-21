using Microsoft.EntityFrameworkCore;
using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.Infrastructure.Persistence;

namespace HealthyFac.Core.Infrastructure.Database;

/// <summary>
/// Database context for the core service. Use cases reach it through <see cref="ICoreDbContext"/>, so they need
/// no reference to this layer. The outbox and inbox reach it through their own interfaces, and
/// <see cref="MessagingExtensions"/> maps every interface to one scoped instance.
/// </summary>
/// <param name="options">The options for this context.</param>
public class CoreDbContext(DbContextOptions<CoreDbContext> options)
    : DbContext(options), ICoreDbContext, IOutboxDbContext, IInboxDbContext
{
    public DbSet<Machine> Machines => Set<Machine>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.HaveEnumerationConversion<MachineState>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoreDbContext).Assembly);
        modelBuilder.ApplyOutboxConfiguration();
        modelBuilder.ApplyInboxConfiguration();
        modelBuilder.ToSnakeCaseConventions();
        modelBuilder.HasPostgreSqlExtensions(DatabaseConstants.RequiredPostgresExtensions);
    }
}
