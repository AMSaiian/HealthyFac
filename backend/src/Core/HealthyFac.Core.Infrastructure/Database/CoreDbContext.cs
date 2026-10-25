using Microsoft.EntityFrameworkCore;
using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Departments;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Domain.Roles;
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
    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<EmployeeInvite> EmployeeInvites => Set<EmployeeInvite>();

    public DbSet<Machine> Machines => Set<Machine>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    public Task RunInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default(CancellationToken)
    )
        => RunInTransactionAsync(
            async token =>
            {
                await operation(token);

                return true;
            },
            cancellationToken);

    public async Task<TResult> RunInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        if (ChangeTracker.HasChanges())
        {
            throw PersistenceErrors.UnsavedChangesBeforeTransaction();
        }

        return await Database
            .CreateExecutionStrategy()
            .ExecuteAsync(token => RunTransactionAttemptAsync(operation, token), cancellationToken);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.HaveEnumerationConversion<MachineState>();
        configurationBuilder.HaveEnumerationConversion<EmployeeStatus>();
        configurationBuilder.HaveEnumerationConversion<RoleCode>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoreDbContext).Assembly);
        modelBuilder.ApplyOutboxConfiguration();
        modelBuilder.ApplyInboxConfiguration();
        modelBuilder.ToSnakeCaseConventions();
        modelBuilder.HasPostgreSqlExtensions(DatabaseConstants.RequiredPostgresExtensions);
    }

    private async Task<TResult> RunTransactionAttemptAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken
    )
    {
        ChangeTracker.Clear();
        await using var transaction = await Database.BeginTransactionAsync(cancellationToken);

        var result = await operation(cancellationToken);
        if (Database.CurrentTransaction != transaction)
        {
            throw PersistenceErrors.TransactionEndedByOperation();
        }

        if (ChangeTracker.HasChanges())
        {
            throw PersistenceErrors.UnsavedChangesAfterOperation();
        }

        await transaction.CommitAsync(cancellationToken);

        return result;
    }
}
