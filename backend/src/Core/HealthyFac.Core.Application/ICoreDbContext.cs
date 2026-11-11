using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Drafts;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Core.Domain.WorkOrders;
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

    DbSet<WorkOrder> WorkOrders { get; }

    DbSet<WorkOrderHistory> WorkOrderHistories { get; }

    DbSet<WorkOrderFile> WorkOrderFiles { get; }

    DbSet<ShiftTemplateVersion> ShiftTemplateVersions { get; }

    DbSet<ShiftTemplateBlock> ShiftTemplateBlocks { get; }

    DbSet<ShiftOverride> ShiftOverrides { get; }

    DbSet<ShiftOverrideBlock> ShiftOverrideBlocks { get; }

    /// <summary>
    /// Gives access to the tracking state of an entity. A use case sets the original <c>xmin</c> value of a row from
    /// the row version that the client sent, and marks a parent row modified when only its child rows change.
    /// </summary>
    /// <param name="entity">The tracked entity.</param>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>The tracking entry of the entity.</returns>
    EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
}
