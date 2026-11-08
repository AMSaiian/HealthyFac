using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.UnitTests.CurrentUser.Data;

namespace HealthyFac.Shared.UnitTests.CurrentUser;

public sealed class ActorContextScopeChecksTests
{
    [Fact]
    public void EnsureCompany_WithOtherCompany_ThrowsForbidden()
    {
        var actor = ActorTestData.CompanyActor();

        var act = () => actor.EnsureCompany(ActorTestData.OtherCompanyId);

        act.Should()
            .Throw<ForbiddenAccessException>()
            .WithMessage(CurrentUserErrors.CompanyOutOfScope().Message);
    }

    [Fact]
    public void EnsureCompany_WithOwnCompany_Passes()
    {
        var actor = ActorTestData.CompanyActor();

        var act = () => actor.EnsureCompany(ActorTestData.CompanyId);

        act.Should().NotThrow();
    }
}
