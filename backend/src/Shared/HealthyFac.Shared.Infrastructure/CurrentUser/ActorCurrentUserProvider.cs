using HealthyFac.Shared.Application.CurrentUser;

namespace HealthyFac.Shared.Infrastructure.CurrentUser;

/// <summary>
/// Reads the current user from the actor of the DI scope, so requests, inbox messages, and jobs share one
/// implementation.
/// </summary>
/// <param name="holder">The holder of the actor of the scope.</param>
public sealed class ActorCurrentUserProvider(ActorContextHolder holder) : ICurrentUserProvider
{
    public Guid? TryGetUserId() => holder.Current?.UserId;

    public Guid GetUserId() => TryGetUserId() ?? throw CurrentUserErrors.ActorMissing();

    public Guid? TryGetCompanyId() => holder.Current?.CompanyId;

    public Guid GetCompanyId() => TryGetCompanyId() ?? throw CurrentUserErrors.CompanyMissing();

    public ActorContext? TryGetActor() => holder.Current;
}
