using HealthyFac.Shared.Application.Persistence;

namespace HealthyFac.Auth.Application;

/// <summary>
/// Declares the <c>DbContext</c> members that use cases need. The Infrastructure layer implements it, so the
/// Application layer needs no reference to Infrastructure. The auth service has no business sets yet. Declare every
/// new <c>DbSet</c> here, and copy a <c>DbContext</c> member signature only when a use case calls it.
/// </summary>
public interface IAuthDbContext : ITransactionalDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));
}
