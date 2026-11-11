using HealthyFac.Shared.Application.CurrentUser;

namespace HealthyFac.Auth.IntegrationTests.Locales.Data;

public sealed class FixedCurrentUserProvider(Guid userId) : ICurrentUserProvider
{
    private readonly Guid _userId = userId;

    public Guid? TryGetUserId() => _userId;

    public Guid GetUserId() => _userId;

    public Guid? TryGetCompanyId() => throw new NotSupportedException();

    public Guid GetCompanyId() => throw new NotSupportedException();

    public ActorContext? TryGetActor() => throw new NotSupportedException();

    public ActorContext GetActor() => throw new NotSupportedException();
}
