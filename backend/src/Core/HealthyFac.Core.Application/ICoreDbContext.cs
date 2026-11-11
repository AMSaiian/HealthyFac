using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Shared.Application.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace HealthyFac.Core.Application;

/// <summary>
/// Declares the sets and the <see cref="DbContext"/> members that use cases need. The Infrastructure layer
/// implements it, so the Application layer needs no reference to Infrastructure. Declare every new
/// <see cref="DbSet{TEntity}"/> here. Copy a <see cref="DbContext"/> member signature only when a use case calls it.
/// </summary>
public interface ICoreDbContext : ITransactionalDbContext
{
    DbSet<Company> Companies { get; }

    DbSet<Role> Roles { get; }

    DbSet<Employee> Employees { get; }

    DbSet<EmployeeInvite> EmployeeInvites { get; }

    DbSet<Machine> Machines { get; }

    DbSet<MachineStateHistory> MachineStateHistories { get; }

    DbSet<MachineDraft> MachineDrafts { get; }

    DbSet<MetricDefinitionDraft> MetricDefinitionDrafts { get; }

    DbSet<StatusDefinitionDraft> StatusDefinitionDrafts { get; }

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
}
