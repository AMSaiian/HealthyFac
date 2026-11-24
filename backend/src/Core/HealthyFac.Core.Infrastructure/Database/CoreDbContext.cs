using System.Data;
using HealthyFac.Core.Application;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.Drafts.Proposals;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders.Drafts;
using HealthyFac.Shared.Infrastructure.Messaging;
using HealthyFac.Shared.Infrastructure.Messaging.Receiving;
using HealthyFac.Shared.Infrastructure.Messaging.Sending;
using HealthyFac.Shared.Infrastructure.Persistence;
using HealthyFac.Shared.Infrastructure.Persistence.Enumerations;
using Microsoft.EntityFrameworkCore;

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

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Employee> Employees => Set<Employee>();

    public DbSet<EmployeeInvite> EmployeeInvites => Set<EmployeeInvite>();

    public DbSet<Machine> Machines => Set<Machine>();

    public DbSet<MachineStateHistory> MachineStateHistories => Set<MachineStateHistory>();

    public DbSet<DraftProposal> DraftProposals => Set<DraftProposal>();

    public DbSet<MachineDraft> MachineDrafts => Set<MachineDraft>();

    public DbSet<WorkOrderDraft> WorkOrderDrafts => Set<WorkOrderDraft>();

    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();

    public DbSet<WorkOrderHistory> WorkOrderHistories => Set<WorkOrderHistory>();

    public DbSet<WorkOrderFile> WorkOrderFiles => Set<WorkOrderFile>();

    public DbSet<ShiftTemplateVersion> ShiftTemplateVersions => Set<ShiftTemplateVersion>();

    public DbSet<ShiftTemplateBlock> ShiftTemplateBlocks => Set<ShiftTemplateBlock>();

    public DbSet<ShiftOverride> ShiftOverrides => Set<ShiftOverride>();

    public DbSet<ShiftOverrideBlock> ShiftOverrideBlocks => Set<ShiftOverrideBlock>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default(CancellationToken)
    ) => TransactionRunner.SaveAsync(this, base.SaveChangesAsync, acceptAllChangesOnSuccess, cancellationToken);

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        TransactionRunner.Save(this, base.SaveChanges, acceptAllChangesOnSuccess);

    public Task RunInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default(CancellationToken)
    )
        => RunInTransactionAsync(operation, IsolationLevel.ReadCommitted, cancellationToken);

    public Task<TResult> RunInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default(CancellationToken)
    )
        => RunInTransactionAsync(operation, IsolationLevel.ReadCommitted, cancellationToken);

    public Task RunInTransactionAsync(
        Func<CancellationToken, Task> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default(CancellationToken)
    )
        => RunInTransactionAsync(
            async token =>
            {
                await operation(token);

                return true;
            },
            isolationLevel,
            cancellationToken);

    public Task<TResult> RunInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default(CancellationToken)
    )
        => TransactionRunner.RunAsync(this, operation, isolationLevel, cancellationToken);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.HaveEnumerationConversion<MachineState>();
        configurationBuilder.HaveEnumerationConversion<EmployeeStatus>();
        configurationBuilder.HaveEnumerationConversion<RoleCode>();
        configurationBuilder.HaveEnumerationConversion<DraftState>();
        configurationBuilder.HaveEnumerationConversion<DraftSource>();
        configurationBuilder.HaveEnumerationConversion<WorkOrderStatus>();
        configurationBuilder.HaveEnumerationConversion<WorkOrderPriority>();
        configurationBuilder.HaveEnumerationConversion<WorkOrderCategory>();
        configurationBuilder.HaveEnumerationConversion<Weekday>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(CoreInfrastructureAssembly.Instance);
        modelBuilder.ApplyOutboxConfiguration();
        modelBuilder.ApplyInboxConfiguration();
        modelBuilder.UseCaseInsensitiveText();
        modelBuilder.ToSnakeCaseConventions();
        modelBuilder.HasPostgreSqlExtensions(DatabaseConstants.RequiredPostgresExtensions);
    }
}
