using HealthyFac.Shared.Application.CurrentUser;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Database.Data;

/// <summary>
/// Creates scopes that run as a user of one company, so a pre-handler check sees the actor
/// that a request would carry.
/// </summary>
public static class UserScopeFixtureExtensions
{
    extension(CoreDatabaseFixture fixture)
    {
        public IServiceScope CreateUserScope(Guid companyId) =>
            fixture.CreateActorScope(
                new ActorContext
                {
                    Kind = ActorKind.User,
                    UserId = Guid.CreateVersion7(),
                    CompanyId = companyId,
                });
    }
}
